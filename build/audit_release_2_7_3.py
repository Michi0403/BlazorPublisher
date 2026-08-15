#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.3 logging/recording recovery."""
from pathlib import Path
import json,re,sys
ROOT=Path(__file__).resolve().parents[1]
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def require(rel, needle):
    if needle not in text(rel): raise AssertionError(f"{rel} missing: {needle}")
def forbid(rel, needle):
    if needle in text(rel): raise AssertionError(f"{rel} unexpectedly contains: {needle}")
try:
    for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel,'<Version>2.7.4</Version>')
    for rel in (
        'src/PublisherStudio.Web/BusinessObjects/ApplicationLogEntry.cs',
        'src/PublisherStudio.Web/BusinessObjects/FileLoggerCoreOptions.cs',
        'src/PublisherStudio.Web/BusinessObjects/LoggingCoreOptions.cs',
        'src/PublisherStudio.Web/BusinessObjects/Enums/CoreLogLevel.cs',
        'src/PublisherStudio.Web/Logging/FileLogger.cs',
        'src/PublisherStudio.Web/Logging/FileLoggerProvider.cs',
        'src/PublisherStudio.Web/Services/LoggingConfigurationService.cs'):
        if not (ROOT/rel).is_file(): raise AssertionError(f'missing logging source {rel}')
    require('src/PublisherStudio.Web/Logging/FileLogger.cs','Path.Combine(Directory.GetCurrentDirectory(), "PublisherStudio.log")')
    require('src/PublisherStudio.Web/Services/LoggingConfigurationService.cs','new FileLoggerProvider(provider.GetRequiredService<IOptionsMonitor<FileLoggerCoreOptions>>()')
    forbid('src/PublisherStudio.Web/Logging/FileLogger.cs',' static ')
    forbid('src/PublisherStudio.Web/Logging/FileLoggerProvider.cs',' static ')
    forbid('src/PublisherStudio.Web/BusinessObjects/LoggingCoreOptions.cs',' const ')
    forbid('src/PublisherStudio.Web/BusinessObjects/FileLoggerCoreOptions.cs',' const ')
    require('src/PublisherStudio.Web/Program.cs','new LoggingConfigurationService(builder.Services, builder.Configuration, startupLogger).Configure(builder.Logging);')
    require('src/PublisherStudio.Web/Program.cs','builder.Logging.AddFilter("Microsoft", LogLevel.Warning);')
    require('src/PublisherStudio.Web/Program.cs','builder.Logging.AddFilter("System", LogLevel.Warning);')
    forbid('src/PublisherStudio.Web/Program.cs','level >= LogLevel.Warning')
    config=json.loads(text('src/PublisherStudio.Web/appsettings.json'))
    file_core=config['LoggingCore']['FileCore']
    assert config['LoggingCore']['CoreLogLevel']==2 and file_core['CoreLogLevel']==2 and file_core['FilePath']==''

    media=text('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor')
    for needle in (
        '@inject ILogger<MediaStudio> Logger',
        'getMediaRecordingState',
        'BrowserMediaRecordingState',
        'ApplyRetainedRecordingInfo(retainedRecording)',
        'InvokeAsync<RetainedMediaRecordingInfo?>',
        'stopMediaRecording',
        './js/mediaStudioInterop.js?v=2.7.4'):
        if needle not in media: raise AssertionError(f'MediaStudio.razor missing {needle}')

    js=text('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','export async function stopMediaRecording(id, dotnet)')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','state.recorder.requestData()')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','state.retainedRecordingInfo = retainedInfo;')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','export function getMediaRecordingState(id, dotnet)')
    stop=re.search(r'export async function stopMediaRecording\(id, dotnet\) \{ try \{(?P<body>.*?)\n \} catch \(__javascriptError\)',js,re.S)
    if not stop: raise AssertionError('could not isolate stopMediaRecording')
    if 'releaseRecordingCapture(state)' in stop.group('body'):
        raise AssertionError('stopMediaRecording still releases capture tracks before recorder finalization')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','finally {\n            releaseRecordingCapture(state);')
    require('build/audit_service_resilience.py','iterator/yield')
    print('PublisherStudio 2.7.3 logging/recording recovery source audit passed.')
except Exception as exc:
    print(f'PublisherStudio 2.7.3 source audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
