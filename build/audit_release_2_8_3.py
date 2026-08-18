#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.8.8 text-service ownership build repair."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    return (root / rel).read_text(encoding="utf-8-sig")

def require(rel: str, *needles: str) -> None:
    global checks
    value = read(rel)
    missing = [needle for needle in needles if needle not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")
    checks += len(needles)

def forbid(rel: str, *needles: str) -> None:
    global checks
    value = read(rel)
    found = [needle for needle in needles if needle in value]
    if found:
        raise AssertionError(f"{rel} unexpectedly contains: {', '.join(found)}")
    checks += len(needles)

try:
    for rel in (
        "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
        "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    ):
        require(rel, "<Version>2.8.8</Version>")

    require(
        "src/PublisherStudio.Web/Components/Editor/PageSurface.razor",
        "@inject PublicationEditorTextService EditorText",
        "EditorText.BuildCanvasSelectionKey(SelectionVisualsEnabled, InteractionEnabled, selectedElementIds)",
    )
    forbid(
        "src/PublisherStudio.Web/Components/Editor/PageSurface.razor",
        'var selectionKey = $"{SelectionVisualsEnabled}|{InteractionEnabled}|{string.Join(",", selectedElementIds)}";',
    )
    require(
        "src/PublisherStudio.Web/Services/PublicationEditorTextService.cs",
        "public sealed class PublicationEditorTextService",
        "public string BuildCanvasSelectionKey",
        'string.Join(",", selectedElementIds ?? Array.Empty<string>())',
        "OrderBy(id => id)",
        'id.ToString("D")',
        "catch (Exception exception)",
        "_logger.LogError(exception",
    )
    require(
        "src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs",
        "AddSingleton<PublicationEditorTextService, PublicationEditorTextService>(services);",
    )

    # Preserve the interaction-suspension fix which motivated the selection key.
    require(
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        'SelectionVisualsEnabled="@MainframeSelectionVisualsEnabled"',
        'InteractionEnabled="@MainframeSelectionVisualsEnabled"',
        "!_pictureEditorVisible",
        "!_dataManagerVisible",
        "!_mediaStudioVisible",
        "!_mediaConverterVisible",
        "!_panelStudioVisible",
    )
    require(
        "src/PublisherStudio.Web/Components/Editor/PageSurface.razor",
        "interactionEnabled = InteractionEnabled",
        "if (!InteractionEnabled) return;",
        "canvas-interaction-suspended",
    )

    # Preserve the slider, converter, and rendered-video work from 2.8.2.
    require(
        "src/PublisherStudio.Web/wwwroot/js/publisherInterop.js",
        "function installStableNativeRangeLifecycle()",
        "requestAnimationFrame(() =>",
        "__publisherRangeCoalesced",
    )
    require(
        "src/PublisherStudio.Web/Components/Editor/MediaConverterStudio.razor",
        'list="ffmpeg-encoder-presets"',
        'list="ffmpeg-pixel-formats"',
        "ApplyPresetGuidance(overwriteEncoderFields: false)",
    )
    require(
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        'Text=\'@LT("Rendered video")\'',
        'Text=\'@LT("Render selected range to video…")\'',
        '"startRenderedVideoExport"',
    )
    require(
        "src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js",
        "async function runRenderedVideoExport",
        "canvas.captureStream(frameRate)",
        "window.publisherVideoEffects.install(runtimeKey, video, canvas, renderConfig)",
    )

    # Active assets carry the release cache token without changing render boundaries.
    require(
        "src/PublisherStudio.Web/Components/App.razor",
        "css/site.css?v=20260818-288",
        "videoEffectRuntime.js?v=2.8.8",
        "publisherInterop.js?v=2.8.8",
    )
    for rel in (
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
    ):
        require(rel, "mediaStudioInterop.js?v=2.8.8")

    expected_render_files = {
        "src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor",
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        "src/PublisherStudio.Web/Components/Pages/Help.razor",
        "src/PublisherStudio.Web/Components/Pages/Localization.razor",
        "src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor",
    }
    actual_render_files = {
        p.relative_to(root).as_posix()
        for p in (root / "src/PublisherStudio.Web").rglob("*.razor")
        if "@rendermode" in p.read_text(encoding="utf-8", errors="ignore")
    }
    if actual_render_files != expected_render_files:
        raise AssertionError(f"render-mode boundary set changed: {sorted(actual_render_files)}")
    checks += len(expected_render_files)

    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")

    # Mirror the exact failing policy intent: PageSurface must not directly own the new string.Join selection-key manipulation.
    page_surface = read("src/PublisherStudio.Web/Components/Editor/PageSurface.razor")
    selection_lines = [line.strip() for line in page_surface.splitlines() if "selectionKey" in line]
    if any("string.Join" in line for line in selection_lines):
        raise AssertionError("PageSurface still owns direct string.Join selection-key manipulation")
    checks += 1

    print(f"PublisherStudio 2.8.8 text-service ownership source audit passed: {checks} checks.")
except AssertionError as exc:
    print(f"PublisherStudio 2.8.8 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
