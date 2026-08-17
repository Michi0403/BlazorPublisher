#!/usr/bin/env python3
"""Static release audit for PublisherStudio 2.8.0 adaptive media quality."""
from pathlib import Path
import hashlib
import json
import sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel):
    return (root / rel).read_text(encoding="utf-8-sig", errors="strict")

def require(rel, *needles):
    global checks
    value = read(rel)
    missing = [needle for needle in needles if needle not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")
    checks += len(needles)

try:
    for rel in (
        "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
        "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    ):
        require(rel, "<Version>2.8.0</Version>")

    require(
        "src/PublisherStudio.Web/BusinessObjects/PublicationStreamingModels.cs",
        "PublicationAdaptiveMediaSettings",
        "AdaptVideo",
        "AdaptAudio",
        "UseProviderKnowledge",
        "UseBrowserCapabilityProbe",
        "PreserveNativeResolution",
        "AllowFrameRateReduction",
        "AllowResolutionReduction",
    )
    require(
        "src/PublisherStudio.Web/Services/Streaming/MediaQualityRecommendationService.cs",
        "RecommendVideoBitrateKbps",
        "RecommendAudioBitrateKbps",
        "RecommendProviderOutput",
        "RecommendLan",
    )
    require(
        "src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js",
        "navigator.mediaCapabilities.encodingInfo",
        "mediaCapabilitiesVideoContentType",
        "mediaCapabilitiesAudioConfiguration",
        "videoTrack.getSettings",
        "metadataPosterMaximumPixels",
        "metadataAnalysisDelayMilliseconds",
        "releaseRecordingCapture(state)",
        "contentHint = source === 'screen' ? 'detail' : 'motion'",
    )
    require(
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        'private string _recordingSizeMode = "source";',
        "Smart adaptive",
        "Adapt video automatically",
        "Adapt audio automatically",
        "Use browser capability probe",
        "Allow smoothness FPS fallback",
        "Allow last-resort resolution fallback",
    )
    require(
        "src/PublisherStudio.Web/Components/Editor/StreamingStudio.razor",
        "UseProviderKnowledge",
        "AdaptVideo",
        "AdaptAudio",
        "MediaQuality.RecommendProviderOutput",
        "MediaQuality.RecommendLan",
    )

    settings = json.loads(read("src/PublisherStudio.Web/appsettings.json"))
    adaptive = settings["PublisherStudio"]["RuntimePolicy"]["MediaSessionDefaults"]["AdaptiveQuality"]
    for key in (
        "Enabled", "DefaultProfile", "ScreenBitsPerPixel", "CameraBitsPerPixel", "MixedBitsPerPixel",
        "ProviderBitsPerPixel", "LanBitsPerPixel", "MinimumAudioBitrateKbps", "AudioBitratePerChannelKbps",
        "MaximumAudioBitrateKbps", "BrowserCodecPreferenceOrder", "FrameRateFallbackRatio",
        "ResolutionFallbackRatio", "MaximumAdaptationAttempts", "MetadataPosterMaximumPixels",
        "MetadataAnalysisDelayMilliseconds",
    ):
        if key not in adaptive:
            raise AssertionError(f"AdaptiveQuality missing configurable policy key {key}")
        checks += 1

    js_path = root / "src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js"
    digest = hashlib.sha256(js_path.read_bytes()).hexdigest()
    manifest = read("build/javascript-diagnostics-files.sha256")
    expected = f"{digest}  src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js"
    if expected not in manifest:
        raise AssertionError("Media Studio JavaScript SHA-256 manifest is stale")
    checks += 1

    render_files = [p for p in (root / "src/PublisherStudio.Web").rglob("*.razor") if "@rendermode" in p.read_text(encoding="utf-8", errors="ignore")]
    if len(render_files) != 5:
        raise AssertionError(f"expected 5 explicit @rendermode files, found {len(render_files)}")
    checks += 1

    print(f"PublisherStudio 2.8.0 adaptive-media source audit passed: {checks} checks.")
except (AssertionError, KeyError, json.JSONDecodeError) as exc:
    print(f"PublisherStudio 2.8.0 adaptive-media source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
