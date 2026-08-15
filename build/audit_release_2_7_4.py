#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.4 logging maintenance repair."""
from pathlib import Path
import json, re, sys
ROOT=Path(__file__).resolve().parents[1]
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def require(rel, needle):
    if needle not in text(rel): raise AssertionError(f"{rel} missing: {needle}")
def forbid(rel, needle):
    if needle in text(rel): raise AssertionError(f"{rel} unexpectedly contains: {needle}")
try:
    for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel,'<Version>2.7.7</Version>')
    for rel in (
        'src/PublisherStudio.Web/BusinessObjects/ApplicationLogEntry.cs',
        'src/PublisherStudio.Web/BusinessObjects/FileLoggerCoreOptions.cs',
        'src/PublisherStudio.Web/BusinessObjects/LoggingCoreOptions.cs',
        'src/PublisherStudio.Web/BusinessObjects/LoggerNullScope.cs',
        'src/PublisherStudio.Web/Services/Logging/FileLogger.cs',
        'src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs',
        'src/PublisherStudio.Web/Services/LoggingConfigurationService.cs'):
        if not (ROOT/rel).is_file(): raise AssertionError(f'missing logging source {rel}')
        logger=text('src/PublisherStudio.Web/Services/Logging/FileLogger.cs')
    if 'Path.Combine(Directory.GetCurrentDirectory(), "PublisherStudio.log")' not in logger:
        raise AssertionError('blank FilePath no longer mirrors LocalGPT current-runtime-directory behavior')
    forbid('src/PublisherStudio.Web/BusinessObjects/FileLoggerCoreOptions.cs','ResolvePath(')
    forbid('src/PublisherStudio.Web/BusinessObjects/FileLoggerCoreOptions.cs','MaxQueueLength')
    forbid('src/PublisherStudio.Web/Services/Logging/FileLogger.cs',' static ')
    forbid('src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs',' static ')
    forbid('src/PublisherStudio.Web/BusinessObjects/LoggingCoreOptions.cs',' const ')
    forbid('src/PublisherStudio.Web/BusinessObjects/FileLoggerCoreOptions.cs',' const ')
    require('src/PublisherStudio.Web/Services/LoggingConfigurationService.cs','new FileLoggerProvider(provider.GetRequiredService<IOptionsMonitor<FileLoggerCoreOptions>>()')
    require('src/PublisherStudio.Web/Services/LoggingConfigurationService.cs','Blank FilePath writes PublisherStudio.log beside the running application.')
    config=json.loads(text('src/PublisherStudio.Web/appsettings.json'))
    file_core=config['LoggingCore']['FileCore']
    assert config['LoggingCore']['CoreLogLevel']==2 and file_core['CoreLogLevel']==2 and file_core['FilePath']==''
    assert 'MaxQueueLength' not in file_core
    require('src/PublisherStudio.InstallerConsole/Program.cs','WorkingDirectory = Path.GetDirectoryName(exePath)')
    # The 2.7.3 recording recovery remains present and is not regressed by this maintenance-only release.
    js=text('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','state.recorder.requestData()')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','finally {\n            releaseRecordingCapture(state);')
    require('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor','getMediaRecordingState')
    print('PublisherStudio 2.7.4 logging maintenance source audit passed.')
except Exception as exc:
    print(f'PublisherStudio 2.7.4 source audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
