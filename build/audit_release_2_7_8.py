#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.9 story-source recovery and 1-Wire compatibility."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]


def text(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8-sig")


def require(rel: str, needle: str) -> None:
    if needle not in text(rel):
        raise AssertionError(f"{rel} missing: {needle}")


def forbid(rel: str, needle: str) -> None:
    if needle in text(rel):
        raise AssertionError(f"{rel} unexpectedly contains: {needle}")


try:
    for rel in (
        "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
        "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    ):
        require(rel, "<Version>2.7.9</Version>")

    require("src/PublisherStudio.Web/Components/App.razor", "publisherInterop.js?v=2.7.9")
    for rel in (
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
    ):
        require(rel, "mediaStudioInterop.js?v=2.7.9")

    factory = "src/PublisherStudio.Web/Services/PublisherDocumentFactory.cs"
    rich = "src/PublisherStudio.Web/Services/RichTextDocumentFactory.cs"
    files = "src/PublisherStudio.Web/Services/PublicationFileService.cs"
    story = "src/PublisherStudio.Web/Components/Editor/StoryEditor.razor"

    require(factory, "RichTextDocumentFactory richTextFactory")
    require(factory, "DocumentContent = richTextFactory.CreateOpenXmlFromPreviewHtml(defaults.TitlePreviewHtml)")
    forbid(factory, "DocumentContent = []")

    require(rich, "public byte[] CreateOpenXmlFromPreviewHtml(string? previewHtml)")
    require(rich, "WebUtility.HtmlDecode")
    require(rich, "HtmlFragmentToPlainText")
    require(rich, "return CreateOpenXmlFromPlainText(HtmlFragmentToPlainText(safeHtml));")

    require(files, "text.DocumentContent = _richTextFactory.CreateOpenXmlFromPreviewHtml(text.PreviewHtml);")
    forbid(files, '_richTextFactory.CreateOpenXml("Text frame")')

    require(story, "var hasStoredStoryContent = TextFrame.DocumentContent is { Length: > 0 };")
    require(story, ": RichTextFactory.CreateOpenXmlFromPreviewHtml(TextFrame.PreviewHtml);")
    require(story, "_legacyConversionPending = hasStoredStoryContent && TextFrame.StoryFormat == StoryStorageFormat.Html;")

    # Saved rich-edit content must stay authoritative. Recovery is only entered for absent payloads.
    deserialize = text(files)
    empty_guard = re.search(
        r"if \(text\.DocumentContent is null \|\| text\.DocumentContent\.Length == 0\)\s*\{(?P<body>.*?)\n\s*\}",
        deserialize,
        re.S,
    )
    if not empty_guard or "CreateOpenXmlFromPreviewHtml(text.PreviewHtml)" not in empty_guard.group("body"):
        raise AssertionError("missing preview-backed empty-story recovery guard")

    # Keep the explicitly reviewed render-mode boundaries intact; child editor components inherit the circuit.
    components = ROOT / "src/PublisherStudio.Web/Components"
    render_entries = []
    for path in components.rglob("*.razor"):
        for line in path.read_text(encoding="utf-8-sig").splitlines():
            if "@rendermode" in line:
                render_entries.append((path.relative_to(ROOT).as_posix(), line.strip()))
    expected = {
        ("src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor", "@rendermode @(new InteractiveServerRenderMode(prerender: false))"),
        ("src/PublisherStudio.Web/Components/Pages/Editor.razor", "@rendermode @(new InteractiveServerRenderMode(prerender: true))"),
        ("src/PublisherStudio.Web/Components/Pages/Help.razor", "@rendermode InteractiveServer"),
        ("src/PublisherStudio.Web/Components/Pages/Localization.razor", "@rendermode InteractiveServer"),
        ("src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor", "@rendermode InteractiveServer"),
    }
    if set(render_entries) != expected:
        raise AssertionError(f"InteractiveServer render boundaries changed: {render_entries!r}")

    # Current LocalGPT 3.0.5-facing Publisher capabilities remain present and the wire package stays pinned.
    organic = "src/PublisherStudio.Web/Services/OrganicPlugins/OrganicCapabilityAndExecutionServices.cs"
    for capability in (
        'Capability("publisher.text.insert.propose"',
        'Capability("publisher.text.edit.request"',
        'Capability("publisher.business-context"',
    ):
        require(organic, capability)
    require(story, 'RequestedOrganicCapabilities = ["publisher.business-context", "publisher.text.insert.propose"]')
    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")

    print("PublisherStudio 2.7.9 story-source recovery and 1-Wire compatibility source audit passed.")
except Exception as exc:
    print(f"PublisherStudio 2.7.9 source audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
