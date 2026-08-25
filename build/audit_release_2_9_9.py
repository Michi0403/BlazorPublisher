#!/usr/bin/env python3
"""Source-only release audit for PublisherStudio 2.9.9."""
from __future__ import annotations
from pathlib import Path
import hashlib, json, re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src/PublisherStudio.Web"
FAIL: list[str] = []
PASS: list[str] = []

def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8-sig")

def require(text: str, token: str, label: str) -> None:
    (PASS if token in text else FAIL).append(label if token in text else f"{label}: missing {token}")

def forbid(text: str, token: str, label: str) -> None:
    (PASS if token not in text else FAIL).append(label if token not in text else f"{label}: forbidden {token}")

# Identity, rollover policy, dependency pin and browser cache identity.
for rel in ["src/PublisherStudio.Web/PublisherStudio.Web.csproj", "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj"]:
    require(read(rel), "<Version>2.9.9</Version>", f"2.9.9 identity in {rel}")
package = json.loads(read("src/PublisherStudio.Web/package.json"))
lock = json.loads(read("src/PublisherStudio.Web/package-lock.json"))
if package.get("version") == lock.get("version") == lock.get("packages", {}).get("", {}).get("version") == "2.9.9": PASS.append("npm release identity")
else: FAIL.append("npm release identity is not 2.9.9")
major, minor, patch = map(int, package["version"].split("."))
if minor < 10 and patch < 10: PASS.append("single-digit minor/patch policy")
else: FAIL.append("release identity violates single-digit minor/patch policy")
project = read("src/PublisherStudio.Web/PublisherStudio.Web.csproj")
require(project, "<DevExpressVersion>25.2.9</DevExpressVersion>", "DevExpress 25.2.9 pin")
app = read("src/PublisherStudio.Web/Components/App.razor")
for asset in ["site.css", "localizationRuntime.js", "videoEffectRuntime.js", "componentRuntime.js", "publisherInterop.js"]:
    require(app, f"{asset}?v=2.9.9", f"2.9.9 cache marker {asset}")

media = read("src/PublisherStudio.Web/Components/Editor/MediaStudio.razor")
media_js = read("src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js")
publisher_js = read("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js")
inspector = read("src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor")

# The exact 2.9.8 replacement regression must stay closed.
for token, label in [
    ("if (!IsVideo || !HasRetainedRecording || _retainedRecordingCommittedToSequence) return;", "first recording also requires sequence ownership"),
    ('LT("Recording completed. Insert it as the first sequence clip before recording again.")', "first recording protection prompt"),
    ("if (HasRetainedRecording && !_retainedRecordingCommittedToSequence)", "uncommitted retained recording start guard"),
    ("_recordingStartPlacementRequired = true;", "pre-capture placement chooser state"),
    ("_plannedRecordingPlacementBoundary = Math.Clamp(_recordingStartPlacementBoundary, 0, _segments.Count);", "pre-capture boundary persistence"),
    ('LT("Start recording here")', "pre-capture start action"),
    ("TimelineEdits.SegmentTimelineStart(_segments, boundary, _playbackRate)", "canonical sequence boundary projection"),
    ("TimelineEdits.InsertAt(_segments, _playbackRate, insertionTimeline, recordedSegment);", "canonical sequence insertion"),
    ("CurrentFieldsAsSegment(inheritSelectedEdits: false)", "fresh recording edit isolation"),
    ("_retainedRecordingCommittedToSequence = true;", "C# retained recording commit marker"),
    ('throw new InvalidOperationException(LT("A video recording must have an explicit sequence position before it can be embedded."));', "no video replacement fallback"),
]: require(media, token, label)
forbid(media, "_segments.Count == 0 || !HasRetainedRecording || !string.IsNullOrWhiteSpace(_dataUrl)", "old placement skip guard")

# Browser-retained Blob commit identity survives reconnect/metadata enrichment.
for token, label in [
    ("retainedRecordingCommitted: false", "browser retained commit state"),
    ("state.retainedRecordingCommitted = false;", "new retained recording starts uncommitted"),
    ("committedToSequence: false", "retained DTO starts uncommitted"),
    ("committedToSequence: Boolean(state.retainedRecordingCommitted || retainedInfo.committedToSequence)", "metadata enrichment preserves commit"),
    ("state.retainedRecordingCommitted = true;", "browser marks successful canonical embed"),
    ("state.retainedRecordingInfo = { ...state.retainedRecordingInfo, committedToSequence: true };", "recovery DTO marks canonical embed"),
]: require(media_js, token, label)
require(media, "public bool CommittedToSequence { get; set; }", "C# retained commit DTO")

# Resolution/aspect concepts reuse Panel / Div presets and add video standards.
for token, label in [
    ("@inject IPanelStudioPreviewPresetService PreviewPresets", "Panel Studio preview preset service reuse"),
    ('value="video-8k"', "8K preset"), ('value="video-4k"', "4K preset"),
    ('value="video-dci4k"', "DCI 4K preset"), ('value="video-vertical4k"', "vertical 4K preset"),
    ('value="video-square2k"', "square 2K preset"),
    ('LT("Panel / Div viewport presets")', "Panel Div preset group"),
    ("PreviewPresets.GetPresets()", "Panel Div maintained preset retrieval"),
    ("private string AspectRatioLabel(int width, int height)", "aspect-ratio labeling"),
    ('"video-8k" => (7680, 4320, "8K UHD")', "8K dimensions"),
]: require(media, token, label)

# Fractional and high-refresh frame rates must not be truncated in JS.
for token, label in [
    ('<option value="23.976">23.976 fps</option>', "23.976 preset"),
    ('<option value="29.97">29.97 fps</option>', "29.97 preset"),
    ('<option value="59.94">59.94 fps</option>', "59.94 preset"),
    ('<option value="240">240 fps</option>', "240 FPS preset"),
    ('step="0.001"', "fractional custom FPS input"),
]: require(media, token, label)
for token, label in [
    ("const positiveNumber = value =>", "fractional JS recording normalization"),
    ("frameRate: positiveNumber(options?.frameRate)", "fractional requested FPS retained"),
    ("maximumFrameRate: positiveNumber(options?.maximumFrameRate)", "fractional adaptive ceiling retained"),
    ("const requestedFrameRate = normalized.frameRate > 0 ? normalized.frameRate : normalized.maximumFrameRate;", "adaptive FPS preset reaches capture constraints"),
]: require(media_js, token, label)
settings = json.loads(read("src/PublisherStudio.Web/appsettings.json"))["PublisherStudio"]["RuntimePolicy"]["MediaSessionDefaults"]
if settings.get("MaximumWidth") == 7680 and settings.get("MaximumHeight") == 4320 and settings.get("MaximumFrameRate") == 240: PASS.append("8K/240 runtime policy")
else: FAIL.append(f"unexpected media maximums: {settings.get('MaximumWidth')}x{settings.get('MaximumHeight')} @{settings.get('MaximumFrameRate')}")

# Preserve the 2.9.8 fixes rather than trading them away.
for token, label in [
    ("let lastShellWidth = -1;", "Story Editor shell width guard"),
    ("if (layoutChanged) window.dispatchEvent(new Event('resize'));", "Story Editor bounded resize"),
]: require(publisher_js, token, label)
forbid(publisher_js, "resizeObserver?.observe(host);", "Story Editor self-observation remains removed")
for token, label in [
    ('draggable="true"', "Mainframe layer row drag"),
    ("State.SetSelectedLayerPosition(oneBasedBackToFrontPosition);", "canonical Mainframe layer reorder"),
]: require(inspector, token, label)
for token, label in [
    ("let mediaCapabilitiesRecordProbeSupported = null;", "Edge record-probe memoization"),
    ("MediaRecorder.isTypeSupported", "MediaRecorder capability fallback"),
    ("stream = canvas.captureStream(0);", "source-driven render capture"),
    ("canvasVideoTrack.requestFrame();", "decoded-frame render request"),
    ("allowFrameRateReduction: false", "render FPS reduction disabled"),
    ("preserveNativeResolution: true", "render native-resolution preservation"),
]: require(media_js, token, label)

# Localization parity and new UI phrases.
locales = ["en-US", "de-DE", "es-ES", "fr-FR", "ja-JP", "uk-UA"]
catalogs = {c: json.loads((WEB / "Localization" / f"{c}.json").read_text(encoding="utf-8-sig")) for c in locales}
keys = [set(catalogs[c]) for c in locales]
if all(k == keys[0] for k in keys[1:]) and len(keys[0]) == 3370: PASS.append("six localization catalogs / 3370-key parity")
else: FAIL.append("localization catalogs are not in exact 3370-key parity")
for culture, catalog in catalogs.items():
    if len({k.casefold() for k in catalog}) == len(catalog): PASS.append(f"{culture} key uniqueness")
    else: FAIL.append(f"{culture} has case-insensitive duplicate keys")
for phrase in ["Place next recording", "Start recording here", "Video standards", "Panel / Div viewport presets", "Frame-rate preset", "Recording completed. Insert it as the first sequence clip before recording again."]:
    if phrase in catalogs["en-US"].values(): PASS.append(f"localized {phrase}")
    else: FAIL.append(f"missing localized phrase {phrase}")

# Browser diagnostics hashes.
manifest = {}
for line in read("build/javascript-diagnostics-files.sha256").splitlines():
    if "  " in line:
        digest, rel = line.split("  ", 1); manifest[rel.strip()] = digest.strip()
js_files = sorted((WEB / "wwwroot/js").glob("*.js"), key=lambda p: p.name.casefold())
if len(js_files) == 16: PASS.append("16 maintained browser JS files")
else: FAIL.append(f"expected 16 maintained browser JS files, got {len(js_files)}")
for path in js_files:
    rel = path.relative_to(ROOT).as_posix()
    data = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n").encode("utf-8")
    digest = hashlib.sha256(data).hexdigest()
    if manifest.get(rel) == digest: PASS.append(f"JS hash {path.name}")
    else: FAIL.append(f"JS hash mismatch {path.name}")

# Render mode stays routed-page-owned.
require(read("src/PublisherStudio.Web/Components/Pages/Editor.razor"), "@rendermode InteractiveServer", "Editor InteractiveServer boundary")
for rel in ["src/PublisherStudio.Web/Components/Editor/MediaStudio.razor", "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor"]:
    forbid(read(rel), "@rendermode", f"no nested render mode {rel}")

# Documentation / release gate evidence.
change = read("CHANGELOG-v2.9.9-VIDEO-SEQUENCE-CAPTURE-PRESETS.md").lower()
for token, label in [
    ("interaction, stacking, input and frontend-failure release gate", "release gate heading"),
    ("canonical object-layer participation", "canonical object checklist"),
    ("selection and layer operations", "selection checklist"),
    ("input routing", "input checklist"), ("local stacking", "stacking checklist"),
    ("preview/export behavior", "preview/export checklist"), ("cleanup", "cleanup checklist"),
    ("diagnostics and recoverable failures", "diagnostics checklist"), ("regression coverage", "regression checklist"),
]: require(change, token, label)
require(read("RELEASE.md"), "PublisherStudio 2.9.9", "RELEASE 2.9.9 identity")

if FAIL:
    print("PublisherStudio 2.9.9 source release audit failed:")
    for item in FAIL: print(" -", item)
    raise SystemExit(1)
print(f"PublisherStudio 2.9.9 source release audit passed: {len(PASS)} checks.")
