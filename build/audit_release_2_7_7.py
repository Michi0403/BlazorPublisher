#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.7 retained recording save-path recovery."""
from pathlib import Path
import hashlib,re,sys
ROOT=Path(__file__).resolve().parents[1]
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def require(rel, needle):
    if needle not in text(rel): raise AssertionError(f"{rel} missing: {needle}")
def forbid(rel, needle):
    if needle in text(rel): raise AssertionError(f"{rel} unexpectedly contains: {needle}")
try:
    for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel,'<Version>2.7.8</Version>')
    media='src/PublisherStudio.Web/Components/Editor/MediaStudio.razor'
    js='src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js'
    require(media,'./js/mediaStudioInterop.js?v=2.7.8')
    require(media,'var shouldRefreshRecordingOwnership = _recordingRecoveryAttemptedSession != SessionId || _recording || _recordingStopping;')
    require(media,'public async Task MediaRecordingMetadataReady(RetainedMediaRecordingInfo info)')
    require(media,'Browser metadata was resolved without blocking Save, Insert, Replace, or Download.')
    require(js,'async function enrichRetainedRecordingMetadata(state, retainedBlob, retainedUrl, retainedInfo, kind)')
    require(js,"metadataWarning: 'Browser metadata analysis is continuing in the background.'")
    require(js,'state.retainedRecordingInfo = retainedInfo;')
    require(js,'state.recordingFinalizing = false;')
    require(js,"Promise.resolve(state.dotnet?.invokeMethodAsync('MediaRecordingReady', retainedInfo))")
    require(js,"void enrichRetainedRecordingMetadata(state, retainedBlob, state.retainedRecordingUrl, retainedInfo, kind)")
    require(js,"await state.dotnet?.invokeMethodAsync('MediaRecordingMetadataReady', enrichedInfo);")
    retain=re.search(r'async function retainRecording\(state, blob, kind\) \{ try \{(?P<body>.*?)\n \} catch \(__javascriptError\)',text(js),re.S)
    if not retain: raise AssertionError('could not isolate retainRecording')
    body=retain.group('body')
    ready=body.find("invokeMethodAsync('MediaRecordingReady'")
    enrich=body.find('enrichRetainedRecordingMetadata')
    if ready < 0 or enrich < 0 or ready > enrich:
        raise AssertionError('completed Blob must be exposed before optional metadata enrichment')
    if 'await inspectElement' in body:
        raise AssertionError('retainRecording must not block Save/Insert/Download on metadata inspection')
    manifest=text('build/javascript-diagnostics-files.sha256')
    normalized=(ROOT/js).read_bytes().replace(b'\r\n',b'\n').replace(b'\r',b'\n')
    digest=hashlib.sha256(normalized).hexdigest()
    if f'{digest}  {js}' not in manifest:
        raise AssertionError('mediaStudioInterop.js diagnostics hash is stale')
    print('PublisherStudio 2.7.7 retained-recording save-path source audit passed.')
except Exception as exc:
    print(f'PublisherStudio 2.7.7 source audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
