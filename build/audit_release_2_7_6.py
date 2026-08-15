#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.6 asynchronous idempotent recording stop."""
from pathlib import Path
import re,sys
ROOT=Path(__file__).resolve().parents[1]
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def require(rel, needle):
    if needle not in text(rel): raise AssertionError(f"{rel} missing: {needle}")
def forbid(rel, needle):
    if needle in text(rel): raise AssertionError(f"{rel} unexpectedly contains: {needle}")
try:
    for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel,'<Version>2.7.6</Version>')
    media='src/PublisherStudio.Web/Components/Editor/MediaStudio.razor'
    js='src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js'
    require(media,'private bool _recordingStopping;')
    require(media,'if (_module is null || !_recording || _recordingStopping) return;')
    require(media,'var stopRequested = await _module.InvokeAsync<bool>(')
    require(media,'browser finalization continues asynchronously')
    require(media,'public bool IsFinalizing { get; set; }')
    require(media,'else if (browserState.IsFinalizing)')
    require(media,'./js/mediaStudioInterop.js?v=2.7.6')
    require(js,'function requestMediaRecordingStop(state)')
    require(js,'recordingStopRequested: false')
    require(js,'recordingFinalizing: false')
    require(js,'export function stopMediaRecording(id, dotnet)')
    require(js,'return requestMediaRecordingStop(state);')
    require(js,'isFinalizing: Boolean(state.recordingFinalizing)')
    require(js,'releaseRecordingCapture(state);\n            state.stream = null;\n            await retainRecording(state, blob, kind);')
    stop=re.search(r'export function stopMediaRecording\(id, dotnet\) \{ try \{(?P<body>.*?)\n \} catch \(__javascriptError\)',text(js),re.S)
    if not stop: raise AssertionError('could not isolate stopMediaRecording')
    if 'while (Date.now()' in stop.group('body') or 'await new Promise' in stop.group('body'):
        raise AssertionError('Stop must not keep the Blazor interop call open while Blob finalization runs')
    print('PublisherStudio 2.7.6 recording-stop source audit passed.')
except Exception as exc:
    print(f'PublisherStudio 2.7.6 source audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
