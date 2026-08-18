#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.8.2 interaction, media converter and rendered-video repairs."""
from __future__ import annotations
from pathlib import Path
import hashlib
import json
import re
import sys

root = Path(__file__).resolve().parents[1]
checks = 0

def text(rel: str) -> str:
    return (root / rel).read_text(encoding="utf-8-sig", errors="strict")

def require(rel: str, *needles: str) -> None:
    global checks
    value = text(rel)
    missing = [needle for needle in needles if needle not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")
    checks += len(needles)

def forbid(rel: str, *needles: str) -> None:
    global checks
    value = text(rel)
    found = [needle for needle in needles if needle in value]
    if found:
        raise AssertionError(f"{rel} unexpectedly contains: {', '.join(found)}")
    checks += len(needles)

def normalized_sha(rel: str) -> str:
    data = (root / rel).read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    return hashlib.sha256(data).hexdigest()

try:
    for rel in (
        "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
        "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    ):
        require(rel, "<Version>2.9.0</Version>")

    # Mainframe canvas remains mounted, but its designer interaction contract is suspended while a studio is open.
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
        'canvas-interaction-suspended',
        "[Parameter] public bool InteractionEnabled { get; set; } = true;",
        "interactionEnabled = InteractionEnabled",
        "if (!InteractionEnabled) return;",
    )
    require(
        "src/PublisherStudio.Web/wwwroot/js/publisherInterop.js",
        "function canvasInteractionEnabled(state)",
        "interactionEnabled: config?.interactionEnabled !== false",
        "if (!canvasInteractionEnabled(state)) return;",
        "if (interactionWasEnabled && !normalizedConfig.interactionEnabled)",
        "resetPointerOperation(state, true);",
        "cancelPendingComponentAction(state);",
        "clearInsertionDrag(state);",
        "clearExternalDropPreview(state);",
    )

    # Native range thumbs remain browser-driven while noisy input notifications are coalesced
    # to the browser's own animation cadence, preventing InteractiveServer event backlogs.
    require(
        "src/PublisherStudio.Web/wwwroot/js/publisherInterop.js",
        "function installStableNativeRangeLifecycle()",
        "requestAnimationFrame(() =>",
        "__publisherRangeCoalesced",
        "document.addEventListener('input'",
        "event.stopPropagation();",
        "document.addEventListener('change'",
        "flush();",
        "event.pointerType === 'mouse' && (event.buttons & 1) === 0",
        "window.addEventListener('blur', release)",
        "if (document.hidden) release()",
    )
    forbid(
        "src/PublisherStudio.Web/wwwroot/js/publisherInterop.js",
        "element.setPointerCapture?.(event.pointerId)",
    )

    # Converter knowledge is smart-preselected from configurable runtime presets and remains editable.
    require(
        "src/PublisherStudio.Web/BusinessObjects/MediaConversionModels.cs",
        "RecommendedVideoEncoderPreset",
        "RecommendedPixelFormat",
        "RecommendedCrf",
        "RecommendedAudioBitrateKbps",
    )
    require(
        "src/PublisherStudio.Web/Components/Editor/MediaConverterStudio.razor",
        'list="ffmpeg-encoder-presets"',
        'list="ffmpeg-pixel-formats"',
        "PublisherRuntimeCollection.FfmpegEncoderPresetSuggestions",
        "PublisherRuntimeCollection.FfmpegPixelFormatSuggestions",
        "ApplyPresetGuidance(overwriteEncoderFields: false)",
        "ApplyPresetGuidance(overwriteEncoderFields: true)",
        "preset.RecommendedVideoEncoderPreset",
        "preset.RecommendedPixelFormat",
        "preset.RecommendedCrf",
        "preset.RecommendedAudioBitrateKbps",
    )
    settings = json.loads(text("src/PublisherStudio.Web/appsettings.json"))
    collections = settings["PublisherStudio"]["RuntimePolicy"]["Collections"]
    for key in ("FfmpegEncoderPresetSuggestions", "FfmpegPixelFormatSuggestions"):
        if not collections.get(key):
            raise AssertionError(f"runtime collection {key} must remain configurable and non-empty")
        checks += 1
    presets = settings["PublisherStudio"]["RuntimePolicy"]["MediaConversionPresets"]
    by_id = {p["Id"]: p for p in presets}
    expected = {
        "webm-vp9": ("yuv420p", 31),
        "webm-vp8": ("yuv420p", 32),
        "mp4-h264": ("yuv420p", 21),
        "video-prores": ("yuv422p10le", None),
    }
    for preset_id, (pixel_format, crf) in expected.items():
        preset = by_id[preset_id]
        if preset.get("RecommendedPixelFormat") != pixel_format:
            raise AssertionError(f"{preset_id} missing configurable pixel-format guidance")
        checks += 1
        if crf is not None:
            if preset.get("RecommendedCrf") != crf:
                raise AssertionError(f"{preset_id} missing configurable CRF guidance")
            checks += 1
    if by_id["mp4-h264"].get("RecommendedVideoEncoderPreset") != "medium":
        raise AssertionError("mp4-h264 missing configurable encoder-preset guidance")
    checks += 1
    # Guard the duplicate-key regression in the actual JSON source.
    audio_webm_match = re.search(r'\{\s*"Id":\s*"audio-webm-opus".*?\n\s*\}', text("src/PublisherStudio.Web/appsettings.json"), re.S)
    if not audio_webm_match or audio_webm_match.group(0).count('"InputKind"') != 1:
        raise AssertionError("audio-webm-opus must contain exactly one InputKind key")
    checks += 1
    require(
        "src/PublisherStudio.Web/wwwroot/css/site.css",
        "grid-template-columns:clamp(220px,20vw,330px) minmax(0,1fr)",
        "repeat(auto-fit,minmax(min(100%,320px),1fr))",
        "repeat(auto-fit,minmax(min(100%,190px),1fr))",
        ".media-converter-settings-grid input,.media-converter-settings-grid select,.media-converter-settings-grid textarea{width:100%;box-sizing:border-box}",
    )

    # Video Studio can bake the same HTML/canvas effect graph into an actual downloadable video.
    require(
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        'Text=\'@LT("Rendered video")\'',
        'Text=\'@LT("Render selected range to video…")\'',
        "BuildVideoEffectConfiguration(RecordingTargetWidth, RecordingTargetHeight)",
        "BuildBrowserRecordingOptions()",
        '"startRenderedVideoExport"',
        '"cancelRenderedVideoExport"',
        "RenderedVideoExportReady",
        "RenderedVideoExportFailed",
        "VideoLayerPayload",
    )
    require(
        "src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js",
        "async function runRenderedVideoExport",
        "canvas.captureStream(frameRate)",
        "window.publisherVideoEffects.install(runtimeKey, video, canvas, renderConfig)",
        "audioContext.createMediaElementSource(video)",
        "audioContext.createMediaStreamDestination()",
        "probeAdaptiveRecordingProfile(stream, 'mixed', options)",
        "new MediaRecorder(stream, recorderOptions)",
        "downloadBlob(blob, fileName)",
        "export function startRenderedVideoExport",
        "export function cancelRenderedVideoExport",
        "stopRenderedVideoExportJob(state.renderExport)",
    )
    # The adaptive-loop bug must stay fixed.
    media_js = text("src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js")
    if "attempt++;\n        attempt++;" in media_js:
        raise AssertionError("adaptive recording still increments an adaptation attempt twice")
    checks += 1

    # Rendering no longer has a baked-in 4K/1080p canvas cap; explicit output dimensions win.
    require(
        "src/PublisherStudio.Web/wwwroot/js/videoEffectRuntime.js",
        "currentConfig.outputWidth",
        "currentConfig.outputHeight",
        "currentConfig.maximumPixels",
        "const explicitSize = configuredWidth > 0 && configuredHeight > 0",
    )
    forbid("src/PublisherStudio.Web/wwwroot/js/videoEffectRuntime.js", "1920 * 1080 * 2")

    # Preserve the established layer/effect implementation instead of replacing it with a flattened preview.
    require(
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        "VideoLayerPayload",
        "AddVideoLayer",
        "DuplicateVideoLayer",
        "MoveVideoLayerUp",
        "MoveVideoLayerDown",
    )
    require(
        "src/PublisherStudio.Web/wwwroot/js/videoEffectRuntime.js",
        "normalizedLayers",
        "applyChroma",
        "applyColorWash",
        "applyVignette",
        "applyGrain",
        "drawBlobDepth",
    )

    # Active browser assets must be cache-busted for this release.
    require("src/PublisherStudio.Web/Components/App.razor", "css/site.css?v=20260818-290", "videoEffectRuntime.js?v=2.9.0", "publisherInterop.js?v=2.9.0")
    for rel in (
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
    ):
        require(rel, "mediaStudioInterop.js?v=2.9.0")

    # JavaScript diagnostics inventory must match the maintained files with normalized newlines.
    manifest = text("build/javascript-diagnostics-files.sha256")
    for rel in (
        "src/PublisherStudio.Web/wwwroot/js/publisherInterop.js",
        "src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js",
        "src/PublisherStudio.Web/wwwroot/js/videoEffectRuntime.js",
    ):
        expected_line = f"{normalized_sha(rel)}  {rel}"
        if expected_line not in manifest:
            raise AssertionError(f"JavaScript diagnostics manifest is stale for {rel}")
        checks += 1

    # All translations used by the new render command are present in every maintained locale.
    translated = [
        "Rendered video",
        "Render selected range to video…",
        "Cancel rendered export",
        "Rendering effects to video…",
        "A rendered video export is already running.",
        "Rendered video export could not start",
        "Rendered video exported",
        "Rendered video export failed",
        "Rendering the selected range with the current Video Studio layers and effects. The browser writes only the video canvas and audio stream; player controls and editor chrome are not captured.",
    ]
    locale_dir = root / "src/PublisherStudio.Web/Localization"
    for locale in sorted(locale_dir.glob("*.json")):
        data = json.loads(locale.read_text(encoding="utf-8-sig"))
        for phrase in translated:
            key = "Text." + phrase.replace(" ", "␠")
            if key not in data:
                raise AssertionError(f"{locale.name} missing {key}")
            checks += 1

    # Render boundaries are intentionally untouched: routed children inherit the existing circuit.
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

    # The shared LocalGPT/PublisherStudio 1-Wire protocol remains deliberately unchanged.
    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")

    print(f"PublisherStudio 2.8.2 interaction/converter/rendered-video source audit passed: {checks} checks.")
except (AssertionError, KeyError, json.JSONDecodeError) as exc:
    print(f"PublisherStudio 2.8.2 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
