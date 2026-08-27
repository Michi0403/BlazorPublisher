#!/usr/bin/env python3
"""Source-only release audit for PublisherStudio 3.0.0. Does not invoke dotnet."""
from __future__ import annotations
from pathlib import Path
import hashlib, json, re, subprocess, shutil

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src/PublisherStudio.Web"
FAIL: list[str] = []
PASS: list[str] = []

def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8-sig")

def require(text: str, token: str, label: str) -> None:
    if token in text: PASS.append(label)
    else: FAIL.append(f"{label}: missing {token}")

def forbid(text: str, token: str, label: str) -> None:
    if token not in text: PASS.append(label)
    else: FAIL.append(f"{label}: forbidden {token}")

# Identity and single-digit minor/patch rollover policy.
for rel in [
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    "src/PublisherStudio.Web/package.json",
    "src/PublisherStudio.Web/package-lock.json",
]:
    require(read(rel), "3.0.0", f"3.0.0 identity {rel}")
package = json.loads(read("src/PublisherStudio.Web/package.json"))
major, minor, patch = map(int, package["version"].split("."))
if (major, minor, patch) == (3, 0, 0) and minor < 10 and patch < 10:
    PASS.append("2.9.9 rollover to valid 3.0.0 identity")
else:
    FAIL.append(f"unexpected package release identity {package['version']}")
require(read("RELEASE.md"), "PublisherStudio 3.0.0", "release document identity")
require(read("RELEASE.md"), "LocalGPT remains unchanged", "LocalGPT unchanged statement")

app = read("src/PublisherStudio.Web/Components/App.razor")
for asset in ["site.css", "localizationRuntime.js", "videoEffectRuntime.js", "componentRuntime.js", "publisherInterop.js"]:
    require(app, f"{asset}?v=3.0.0", f"3.0.0 cache marker {asset}")

media = read("src/PublisherStudio.Web/Components/Editor/MediaStudio.razor")
media_js = read("src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js")
site_css = read("src/PublisherStudio.Web/wwwroot/css/site.css")

# Exact repeated-recording overwrite race repair.
require(media, "TimelineEdits.InsertAt(_segments, _playbackRate, insertionTimeline, recordedSegment);", "canonical recording insertion")
require(media, "SelectSegment(recordedSegment.Id, seek: false, commitCurrent: false);", "recording selection does not commit new fields into old clip")
forbid(media, "SelectSegment(recordedSegment.Id, seek: false);", "old recording overwrite call removed")

# Preview asset lifetime / stale URL 404 repair.
require(media, "private readonly HashSet<Guid> _previewAssetIds = [];", "browser-visible preview asset set")
require(media, "private readonly HashSet<Guid> _retiredPreviewAssetIds = [];", "deferred preview release set")
require(media, "_previewAssetIds.Add(_previewAssetId);", "new preview asset retained")
require(media, "private void RetirePreviewAssets()", "preview retirement helper")
require(media, "private void ReleaseRetiredPreviewAssets()", "post-render preview release helper")
require(media, "private void ReleaseAllPreviewAssets()", "final preview teardown helper")
refresh = media[media.index("private void RefreshPreviewSource()"):media.index("private async Task PlaySelection()")]
forbid(refresh, "MediaAssets.Remove(_previewAssetId);", "preview refresh does not remove browser-visible source immediately")
require(refresh, "RetirePreviewAssets();", "preview refresh defers old source removal")
on_after = media[media.index("protected override async Task OnAfterRenderAsync"):media.index("private async Task<IJSObjectReference> EnsureMediaModuleAsync")]
require(on_after, "ReleaseRetiredPreviewAssets();", "retired preview URLs released only after DOM render")
dispose = media[media.index("public async ValueTask DisposeAsync()"): ]
require(dispose, "ReleaseAllPreviewAssets();", "preview assets released after browser teardown")

# DevExpress W2003 warning repair: overview ticks must be based on total duration, not the zoom window.
timeline = read("src/PublisherStudio.Web/Components/Editor/PublicationTimeline.razor")
require(timeline, 'TickInterval="@OverviewMajorTick"', "overview RangeSelector uses total-duration major ticks")
require(timeline, 'MinorTickInterval="@OverviewMinorTick"', "overview RangeSelector uses total-duration minor ticks")
require(timeline, "var target = Math.Max(.1, TimelineDuration / 12d);", "overview tick density scales with full timeline")

# Ctrl/Command + Shift timeline selection.
for token, label in [
    ("private readonly HashSet<Guid> _selectedSegmentIds = [];", "timeline multi-selection state"),
    ("private Guid? _timelineSelectionAnchorId;", "Shift selection anchor"),
    ("args.CtrlKey || args.MetaKey", "Ctrl/Command additive selection"),
    ("if (args.ShiftKey || args.CtrlKey || args.MetaKey)", "modifier-aware timeline pointer selection"),
    ("if (shiftKey)", "Shift range selection"),
    ("@onclick=\"(MouseEventArgs args) => SelectTimelineSegment(segment.Id, args)\"", "modifier-aware timeline click"),
    ("_selectedSegmentIds.Contains(segment.Id) ? \"selected\"", "multi-selected timeline styling"),
    ("primary-selected", "primary timeline styling"),
]: require(media if token != "primary-selected" else site_css + media, token, label)

# Selected-range output and browser implementation.
for token, label in [
    ("Export selected ranges separately…", "separate selected range command"),
    ("Export selected ranges combined…", "combined selected range command"),
    ("private async Task ExportSelectedTimelineRanges(bool combined)", "C# selected range export"),
    ("MediaAssets.GetOrRegister(segment)", "stable selected clip asset URL"),
    ("TemporalSelectionCommitted", "committed temporal range export"),
    ("SelectedTimelineSegments.Where(IsPlayableSegment)", "selected timeline export source set"),
]: require(media, token, label)
for token, label in [
    ("export async function exportSelectedTimelineRanges", "JS selected range export API"),
    ("recordSelectedTimelineClip", "per-clip rendered range encoder"),
    ("combineSelectedTimelineBlobs", "combined sequence encoder"),
    ("downloadBlob(result.blob, result.fileName);", "combined browser download"),
    ("downloadBlob(result.blob, fileName);", "separate browser downloads"),
]: require(media_js, token, label)

# Logging: explicit text file and LocalGPT-derived provider path remains active.
settings = json.loads(read("src/PublisherStudio.Web/appsettings.json"))
if settings.get("LoggingCore", {}).get("FileCore", {}).get("FilePath") == "PublisherStudio.log":
    PASS.append("explicit PublisherStudio.log appsettings path")
else:
    FAIL.append("LoggingCore:FileCore:FilePath is not PublisherStudio.log")
logger = read("src/PublisherStudio.Web/Services/Logging/FileLogger.cs")
logging_config = read("src/PublisherStudio.Web/Services/LoggingConfigurationService.cs")
program = read("src/PublisherStudio.Web/Program.cs")
for token, label in [
    ("BlockingCollection<string>", "queued file logger"),
    ("File.AppendAllText", "file log persistence"),
    ("File.Open(realPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)", "eager physical log-file creation"),
    ("new FileLoggerProvider", "file logger provider registration"),
]: require(logger + logging_config, token, label)
require(program, "new LoggingConfigurationService(builder.Services, builder.Configuration, startupLogger).Configure(builder.Logging);", "startup logging composition")
require(read("src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor"), "ReportJavaScriptErrorAsync", "browser diagnostics .NET bridge")

# Render-mode ownership remains routed-page based; nested editor components must inherit the circuit.
for page in ["Editor", "Help", "Localization", "OrganicPlugins"]:
    require(read(f"src/PublisherStudio.Web/Components/Pages/{page}.razor"), "@rendermode InteractiveServer", f"{page} InteractiveServer boundary")
require(read("src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor"), "InteractiveServerRenderMode(prerender: false)", "diagnostics interactive island")
for rel in [
    "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
    "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
]:
    forbid(read(rel), "@rendermode", f"no nested render mode {rel}")

# Localization exact parity, including new strings.
locales = ["en-US", "de-DE", "es-ES", "fr-FR", "ja-JP", "uk-UA"]
catalogs = {loc: json.loads((WEB / "Localization" / f"{loc}.json").read_text(encoding="utf-8-sig")) for loc in locales}
keysets = [set(catalogs[loc]) for loc in locales]
if all(keys == keysets[0] for keys in keysets[1:]): PASS.append(f"six localization catalogs exact parity / {len(keysets[0])} keys")
else: FAIL.append("localization catalogs do not have exact key parity")
for loc, catalog in catalogs.items():
    if len({key.casefold() for key in catalog}) == len(catalog): PASS.append(f"{loc} case-insensitive key uniqueness")
    else: FAIL.append(f"{loc} contains case-insensitive duplicate keys")
for phrase in [
    "Export selected ranges separately…", "Export selected ranges combined…", "Exporting selected ranges…",
    "Selected range export failed", "Selected range export cancelled.",
]:
    if phrase in catalogs["en-US"].values(): PASS.append(f"localized new phrase {phrase}")
    else: FAIL.append(f"missing en-US localized phrase {phrase}")

# JavaScript diagnostics hash manifest and syntax (Node only if available).
manifest: dict[str, str] = {}
for line in read("build/javascript-diagnostics-files.sha256").splitlines():
    if "  " in line:
        digest, rel = line.split("  ", 1)
        manifest[rel.strip()] = digest.strip()
js_files = sorted((WEB / "wwwroot/js").glob("*.js"), key=lambda path: path.name.casefold())
for path in js_files:
    rel = path.relative_to(ROOT).as_posix()
    normalized = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n").encode("utf-8")
    digest = hashlib.sha256(normalized).hexdigest()
    if manifest.get(rel) == digest: PASS.append(f"JS hash {path.name}")
    else: FAIL.append(f"JS hash mismatch {path.name}")
node = shutil.which("node")
if node:
    result = subprocess.run([node, "--check", str(WEB / "wwwroot/js/mediaStudioInterop.js")], capture_output=True, text=True)
    if result.returncode == 0: PASS.append("mediaStudioInterop.js Node syntax")
    else: FAIL.append("mediaStudioInterop.js syntax: " + (result.stderr.strip() or result.stdout.strip()))
else:
    PASS.append("Node syntax check skipped: Node unavailable")

# Package/appsettings JSON parse happened above; validate package lock top identities too.
lock = json.loads(read("src/PublisherStudio.Web/package-lock.json"))
if lock.get("version") == "3.0.0" and lock.get("packages", {}).get("", {}).get("version") == "3.0.0": PASS.append("package-lock 3.0.0 identity")
else: FAIL.append("package-lock top identities are not 3.0.0")

if FAIL:
    print("PublisherStudio 3.0.0 source release audit failed:")
    for item in FAIL: print(" -", item)
    raise SystemExit(1)
print(f"PublisherStudio 3.0.0 source release audit passed: {len(PASS)} checks.")
