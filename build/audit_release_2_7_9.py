#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.9 browser recording quality controls."""
from pathlib import Path
import hashlib, json, re, sys
ROOT = Path(__file__).resolve().parents[1]

def text(rel):
    return (ROOT / rel).read_text(encoding='utf-8-sig')

def require(rel, needle):
    if needle not in text(rel):
        raise AssertionError(f'{rel} missing: {needle}')

def forbid(rel, needle):
    if needle in text(rel):
        raise AssertionError(f'{rel} unexpectedly contains: {needle}')

try:
    web_project = 'src/PublisherStudio.Web/PublisherStudio.Web.csproj'
    installer_project = 'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'
    for rel in (web_project, installer_project):
        require(rel, '<Version>2.9.0</Version>')

    media = 'src/PublisherStudio.Web/Components/Editor/MediaStudio.razor'
    js = 'src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js'
    policy = 'src/PublisherStudio.Web/BusinessObjects/PublisherRuntimePolicyModels.cs'
    settings = 'src/PublisherStudio.Web/appsettings.json'

    require(media, './js/mediaStudioInterop.js?v=2.9.0')
    require(media, '@LT("Recording capture quality")')
    require(media, '<option value="source">@LT("Source/native")</option>')
    require(media, '<option value="master">@LT("Streaming master")')
    require(media, '<option value="output">@LT("Streaming output")')
    require(media, 'RecordingPolicy.BrowserRecordingVideoBitrateKbps')
    require(media, 'RecordingPolicy.BrowserRecordingAudioBitrateKbps')
    require(media, 'BuildBrowserRecordingOptions()')
    require(media, 'videoBitsPerSecond = _recordingVideoBitrateKbps * 1000')
    require(media, 'audioBitsPerSecond = _recordingAudioBitrateKbps * 1000')
    require(media, 'InvokeAsync<MediaRecordingStartInfo?>')

    require(policy, 'public int BrowserRecordingVideoBitrateKbps { get; init; }')
    require(policy, 'public int BrowserRecordingAudioBitrateKbps { get; init; }')
    require(policy, 'public string BrowserRecordingCodecPreference { get; init; } = "auto";')
    require(settings, '"BrowserRecordingVideoBitrateKbps": 32000')
    require(settings, '"BrowserRecordingAudioBitrateKbps": 192')
    require(settings, '"BrowserRecordingCodecPreference": "auto"')

    require(js, "const vp9 = ['video/webm;codecs=vp9,opus', 'video/webm;codecs=vp9'];")
    require(js, "const vp8 = ['video/webm;codecs=vp8,opus', 'video/webm;codecs=vp8'];")
    require(js, "? (preference === 'vp8' ? [...vp8, ...vp9, 'video/webm'] : [...vp9, ...vp8, 'video/webm'])")
    require(js, 'videoBitsPerSecond = recordingOptions.videoBitsPerSecond')
    require(js, 'audioBitsPerSecond = recordingOptions.audioBitsPerSecond')
    require(js, 'getDisplayMedia({ video: videoConstraints, audio: true })')
    require(js, 'await track.applyConstraints(constraints);')
    require(js, 'width: Number(videoSettings.width) || 0')
    require(js, 'videoBitsPerSecond: Number(state.recorder?.videoBitsPerSecond)')
    forbid(js, "? ['video/webm;codecs=vp8,opus', 'video/webm;codecs=vp8', 'video/webm;codecs=vp9,opus', 'video/webm']")

    appsettings = json.loads(text(settings))
    defaults = appsettings['PublisherStudio']['RuntimePolicy']['MediaSessionDefaults'] if 'PublisherStudio' in appsettings else appsettings['RuntimePolicy']['MediaSessionDefaults']
    if defaults['BrowserRecordingVideoBitrateKbps'] <= defaults['VideoBitrateKbps']:
        raise AssertionError('browser recording bitrate must exceed the streaming output default for high-resolution local capture')

    required_english = {
        'Recording capture quality', 'Capture size', 'Source/native', 'Streaming master', 'Streaming output',
        'Custom', 'Capture width', 'Capture height', 'Capture frame rate', 'Recording codec',
        'Auto (prefer VP9)', 'Recording video bitrate (kbps)', 'Recording audio bitrate (kbps)'
    }
    catalogs = sorted((ROOT / 'src/PublisherStudio.Web/Localization').glob('*.json'))
    en = json.loads((ROOT / 'src/PublisherStudio.Web/Localization/en-US.json').read_text(encoding='utf-8-sig'))
    english_keys = {v: k for k, v in en.items()}
    missing_english = sorted(required_english - english_keys.keys())
    if missing_english:
        raise AssertionError(f'en-US missing new recording strings: {missing_english}')
    for catalog in catalogs:
        data = json.loads(catalog.read_text(encoding='utf-8-sig'))
        missing = [english_keys[value] for value in required_english if english_keys[value] not in data]
        if missing:
            raise AssertionError(f'{catalog.name} missing recording localization keys: {missing}')

    manifest = text('build/javascript-diagnostics-files.sha256')
    normalized = (ROOT / js).read_bytes().replace(b'\r\n', b'\n').replace(b'\r', b'\n')
    digest = hashlib.sha256(normalized).hexdigest()
    if f'{digest}  {js}' not in manifest:
        raise AssertionError('mediaStudioInterop.js diagnostics hash is stale')

    # InteractiveServer boundaries must not drift in a recording-quality-only release.
    current = sorted(str(path.relative_to(ROOT / 'src/PublisherStudio.Web')) for path in (ROOT / 'src/PublisherStudio.Web').rglob('*.razor') if '@rendermode InteractiveServer' in path.read_text(encoding='utf-8-sig'))
    if len(current) != 3:
        raise AssertionError(f'expected the retained 3 explicit InteractiveServer Razor boundaries, found {len(current)}: {current}')

    print('PublisherStudio 2.7.9 recording-quality source audit passed.')
except Exception as exc:
    print(f'PublisherStudio 2.7.9 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
