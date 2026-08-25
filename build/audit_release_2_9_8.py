#!/usr/bin/env python3
"""Source-only release audit for PublisherStudio 2.9.8."""
from __future__ import annotations

from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src/PublisherStudio.Web"
FAILURES: list[str] = []
CHECKS: list[str] = []


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8-sig")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        FAILURES.append(f"missing {label}: {needle}")
    else:
        CHECKS.append(label)


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        FAILURES.append(f"forbidden {label}: {needle}")
    else:
        CHECKS.append(label)


# Release identity and dependency pins.
for rel in [
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
]:
    require(read(rel), "<Version>2.9.8</Version>", f"2.9.8 version in {rel}")

package = json.loads(read("src/PublisherStudio.Web/package.json"))
lock = json.loads(read("src/PublisherStudio.Web/package-lock.json"))
if package.get("version") != "2.9.8" or lock.get("version") != "2.9.8" or lock.get("packages", {}).get("", {}).get("version") != "2.9.8":
    FAILURES.append("npm package/package-lock release identity is not aligned at 2.9.8")
else:
    CHECKS.append("npm package/package-lock 2.9.8 alignment")
major, minor, patch = map(int, package["version"].split("."))
if minor >= 10 or patch >= 10:
    FAILURES.append("release version violates the single-digit minor/patch policy")
else:
    CHECKS.append("single-digit minor/patch release policy")

project = read("src/PublisherStudio.Web/PublisherStudio.Web.csproj")
require(project, "<DevExpressVersion>25.2.9</DevExpressVersion>", "DevExpress 25.2.9 pin")

app = read("src/PublisherStudio.Web/Components/App.razor")
for asset in ["site.css", "localizationRuntime.js", "videoEffectRuntime.js", "componentRuntime.js", "publisherInterop.js"]:
    require(app, f"{asset}?v=2.9.8", f"2.9.8 browser cache marker for {asset}")

# Story Editor caret/layout race regression.
publisher_js = read("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js")
for needle, label in [
    ("let lastShellWidth = -1;", "Story Editor shell-width transition guard"),
    ("const currentHost = state?.host || host;", "Story Editor current RichEdit host resolution"),
    ("if (layoutChanged) window.dispatchEvent(new Event('resize'));", "Story Editor resize only on owning shell change"),
    ("resizeObserver?.observe(shell);", "Story Editor shell-only ResizeObserver"),
]:
    require(publisher_js, needle, label)
for needle, label in [
    ("resizeObserver?.observe(host);", "Story Editor RichEdit-host self observation"),
    ("host.scrollLeft = 0;", "Story Editor forced horizontal scroll reset"),
]:
    forbid(publisher_js, needle, label)

# Mainframe Layers drag/drop must reuse the canonical layer service operation.
inspector = read("src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor")
for needle, label in [
    ('draggable="true"', "Mainframe layer row drag affordance"),
    ('@ondragstart="() => BeginLayerDrag(item)"', "Mainframe layer drag start routing"),
    ('@ondrop="() => DropLayer(item)"', "Mainframe layer drop routing"),
    ("State.SetSelectedLayerPosition(oneBasedBackToFrontPosition);", "canonical selected-layer position operation"),
    ("State.IsSelected(target.Id)", "selected-block self-drop guard"),
    ("OperationalNotifications.Error(ex.Message, LT(\"Layer reorder failed\"), nameof(InspectorPanel));", "layer reorder recoverable notification"),
]:
    require(inspector, needle, label)
forbid(inspector, "z-index: 214748", "application-wide maximum z-index workaround")

# Recording placement/download and canonical sequence insertion.
media = read("src/PublisherStudio.Web/Components/Editor/MediaStudio.razor")
for needle, label in [
    ("private bool CanDownloadCurrentMedia", "downloadable retained-or-selected media predicate"),
    ('LT("Place new recording")', "recording placement prompt"),
    ("private void RequireRecordingPlacementIfNeeded()", "recording placement requirement"),
    ("private async Task ConfirmRecordingPlacement()", "recording placement confirmation"),
    ("TimelineEdits.SegmentTimelineStart(_segments, boundary, _playbackRate)", "canonical recording boundary projection"),
    ("TimelineEdits.InsertAt(_segments, _playbackRate, insertionTimeline, recordedSegment);", "canonical recording sequence insertion"),
    ("CurrentFieldsAsSegment(inheritSelectedEdits: false)", "fresh recording does not inherit selected clip edits"),
    ('InvokeAsync<bool>("downloadCurrentMedia", "media-studio-preview", fileName)', "browser download of retained/current source"),
    ("OperationalNotifications.Success(_warning, LT(\"Media downloaded\"), nameof(MediaStudio));", "media download success notification"),
    ("OperationalNotifications.Error(_error, LT(\"Media download failed\"), nameof(MediaStudio));", "media download failure notification"),
]:
    require(media, needle, label)

policy = json.loads(read("build/async-continuation-policy.json"))
policy_text = json.dumps(policy)
require(policy_text, "ConfirmRecordingPlacement", "renderer-affine recording placement async policy")

# Video rendering / recording quality and browser compatibility regressions.
media_js = read("src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js")
video_runtime = read("src/PublisherStudio.Web/wwwroot/js/videoEffectRuntime.js")
for needle, label in [
    ("export async function downloadCurrentMedia", "current media browser download"),
    ("let mediaCapabilitiesRecordProbeSupported = null;", "record-encoding capability probe memoization"),
    ("async function tryRecordEncodingInfo", "optional MediaCapabilities encoding probe wrapper"),
    ("recordEncodingProbeUnsupported(error)", "unsupported record-enum fallback"),
    ("MediaRecorder.isTypeSupported", "MediaRecorder codec support fallback"),
    ("async function downloadUnmodifiedRenderedSource", "no-effect full-source exact-byte export path"),
    ("async function estimateDecodedVideoFrameRate", "decoded source frame-rate estimator"),
    ("stream = canvas.captureStream(0);", "source-driven zero-rate canvas capture"),
    ("typeof canvasVideoTrack?.requestFrame === 'function'", "source-frame requestFrame capability guard"),
    ("canvasVideoTrack.requestFrame();", "source-frame-driven canvas capture request"),
    ("bitrateFrameRate: fallbackFrameRate", "source cadence used for adaptive bitrate"),
    ("allowFrameRateReduction: false", "effect export frame-rate reduction disabled"),
    ("preserveNativeResolution: true", "effect export native-resolution policy"),
    ("normalizedMimeType.includes('mp4') ? '.mp4'", "direct-source MP4 extension preservation"),
    ("normalizedMimeType.includes('quicktime') ? '.mov'", "direct-source QuickTime extension preservation"),
    ("__javascriptError?.name !== 'NotAllowedError' && __javascriptError?.name !== 'AbortError'", "expected capture-picker cancellation classification"),
    ("render-export:ready-callback", "late rendered-export callback isolation"),
]:
    require(media_js, needle, label)
require(video_runtime, "currentConfig.onFrameRendered", "video effect runtime rendered-frame callback")

settings = json.loads(read("src/PublisherStudio.Web/appsettings.json"))
maximum_frame_rate = settings.get("PublisherStudio", {}).get("RuntimePolicy", {}).get("MediaSessionDefaults", {}).get("MaximumFrameRate")
if maximum_frame_rate != 240:
    FAILURES.append(f"MediaSessionDefaults.MaximumFrameRate is {maximum_frame_rate!r}, expected 240")
else:
    CHECKS.append("240 FPS maintained manual/source ceiling")

# Avoid the exact noisy direct encodingInfo call pattern from the supplied Edge 151 trace.
# encodingInfo may exist only behind tryRecordEncodingInfo, where unsupported enum errors are swallowed once.
direct_calls = [m.start() for m in re.finditer(r"navigator\.mediaCapabilities\.encodingInfo\s*\(", media_js)]
helper_start = media_js.find("async function tryRecordEncodingInfo")
helper_end = media_js.find("async function probeAdaptiveRecordingProfile", helper_start)
if not direct_calls or all(helper_start <= pos < helper_end for pos in direct_calls):
    CHECKS.append("MediaCapabilities encodingInfo calls isolated behind compatibility wrapper")
else:
    FAILURES.append("MediaCapabilities encodingInfo is still called directly outside the compatibility wrapper")

# Localization parity and release phrases.
locales = ["en-US", "de-DE", "es-ES", "fr-FR", "ja-JP", "uk-UA"]
catalogs: dict[str, dict[str, str]] = {}
for culture in locales:
    path = WEB / "Localization" / f"{culture}.json"
    catalogs[culture] = json.loads(path.read_text(encoding="utf-8-sig"))
    lowered = [key.casefold() for key in catalogs[culture]]
    if len(lowered) != len(set(lowered)):
        FAILURES.append(f"{culture} localization catalog contains case-insensitive duplicate keys")
    else:
        CHECKS.append(f"{culture} localization key uniqueness")
keysets = [set(catalogs[culture]) for culture in locales]
if not all(keys == keysets[0] for keys in keysets[1:]):
    FAILURES.append("PublisherStudio localization catalogs are not in exact key parity")
else:
    CHECKS.append(f"six localization catalogs / {len(keysets[0])}-key parity")
for phrase in [
    "Place new recording",
    "Recording position",
    "Insert recording",
    "Keep uninserted",
    "Download selected source",
    "Drag to reorder layer",
    "Layer reorder failed",
    "Media downloaded",
    "Media download failed",
]:
    if phrase not in set(catalogs["en-US"].values()):
        FAILURES.append(f"missing localized English phrase: {phrase}")
    else:
        CHECKS.append(f"localized phrase {phrase}")

# JavaScript diagnostics integrity for every maintained browser module.
manifest_lines = read("build/javascript-diagnostics-files.sha256").splitlines()
manifest = {}
for line in manifest_lines:
    if "  " in line:
        digest, rel = line.split("  ", 1)
        manifest[rel.strip()] = digest.strip()
js_files = sorted((WEB / "wwwroot/js").glob("*.js"), key=lambda path: path.name.casefold())
if len(js_files) != 16:
    FAILURES.append(f"expected 16 maintained browser JS files, found {len(js_files)}")
else:
    CHECKS.append("16 maintained browser JS files")
for path in js_files:
    rel = path.relative_to(ROOT).as_posix()
    normalized = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n").replace("\r", "\n").encode("utf-8")
    digest = hashlib.sha256(normalized).hexdigest()
    if manifest.get(rel) != digest:
        FAILURES.append(f"JavaScript diagnostics hash mismatch for {rel}")
    else:
        CHECKS.append(f"JavaScript diagnostics hash {path.name}")

# Existing render-mode boundary remains authoritative and no nested Studio boundary was added.
editor = read("src/PublisherStudio.Web/Components/Pages/Editor.razor")
require(editor, "@rendermode InteractiveServer", "Editor InteractiveServer render boundary")
for rel in [
    "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
    "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
]:
    forbid(read(rel), "@rendermode", f"nested render mode in {rel}")

# Release documentation and explicit gate checklist.
changelog = read("CHANGELOG-v2.9.8-STORY-CARET-LAYERS-VIDEO-QUALITY-REPAIR.md")
changelog_lower = changelog.lower()
validation = read("VALIDATION-v2.9.8-source.md")
release = read("RELEASE.md")
for needle, label in [
    ("## interaction, stacking, input and frontend-failure release gate", "release gate checklist heading"),
    ("canonical object-layer", "canonical object-layer checklist evidence"),
    ("selection persistence", "selection/layer-operation checklist evidence"),
    ("mouse, pen, touch, keyboard", "input-family checklist evidence"),
    ("preview, html/website, raster/svg, print/pdf and video-render", "export checklist evidence"),
    ("listener, pointer-capture, observer, object-url", "cleanup checklist evidence"),
    ("structured diagnostics", "diagnostics checklist evidence"),
    ("regression", "regression evidence checklist"),
]:
    require(changelog_lower, needle, label)
require(validation, "SOURCE-NOT-COMPILED", "source-only validation disclosure")
require(release, "PublisherStudio 2.9.8", "2.9.8 RELEASE identity")

if FAILURES:
    print("PublisherStudio 2.9.8 source release audit failed:")
    for failure in FAILURES:
        print("  -", failure)
    raise SystemExit(1)
print(f"PublisherStudio 2.9.8 source release audit passed: {len(CHECKS)} checks.")
