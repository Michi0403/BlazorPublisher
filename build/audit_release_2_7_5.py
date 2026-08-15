#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.5 overlay-safe logging repair."""
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
        require(rel,'<Version>2.7.6</Version>')
    canonical=(
        'src/PublisherStudio.Web/Services/Logging/FileLogger.cs',
        'src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs',
        'src/PublisherStudio.Web/Services/LoggingConfigurationService.cs',
        'src/PublisherStudio.Web/BusinessObjects/ApplicationLogEntry.cs',
        'src/PublisherStudio.Web/BusinessObjects/FileLoggerCoreOptions.cs',
        'src/PublisherStudio.Web/BusinessObjects/LoggingCoreOptions.cs',
        'src/PublisherStudio.Web/BusinessObjects/LoggerNullScope.cs')
    for rel in canonical:
        if not (ROOT/rel).is_file(): raise AssertionError(f'missing canonical logging source {rel}')
    require('src/PublisherStudio.Web/Services/Logging/FileLogger.cs','Path.Combine(Directory.GetCurrentDirectory(), "PublisherStudio.log")')
    forbid('src/PublisherStudio.Web/Services/Logging/FileLogger.cs','ExceptionType')
    forbid('src/PublisherStudio.Web/Services/Logging/FileLogger.cs','ExceptionMessage')
    forbid('src/PublisherStudio.Web/Services/Logging/FileLogger.cs','ExceptionStackTrace')
    forbid('src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs','ResolvePath(')
    forbid('src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs','MaxQueueLength')
    require('src/PublisherStudio.Web/Services/LoggingConfigurationService.cs','using PublisherStudio.Services.Logging;')
    require('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<Compile Remove="Logging\\FileLogger.cs;Logging\\FileLoggerProvider.cs" />')
    for rel in ('src/PublisherStudio.Web/Logging/FileLogger.cs','src/PublisherStudio.Web/Logging/FileLoggerProvider.cs'):
        require(rel,'upgrade tombstone')
        forbid(rel,'class FileLogger')
        forbid(rel,'class FileLoggerProvider')
    baseline=json.loads(text('build/logging-baseline.json'))['files']
    for rel in ('src/PublisherStudio.Web/Services/Logging/FileLogger.cs','src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs'):
        if rel not in baseline: raise AssertionError(f'logging baseline missing reviewed infrastructure {rel}')
    config=json.loads(text('src/PublisherStudio.Web/appsettings.json'))
    assert config['LoggingCore']['FileCore']['FilePath']==''
    require('src/PublisherStudio.InstallerConsole/Program.cs','WorkingDirectory = Path.GetDirectoryName(exePath)')
    # Preserve 2.7.3 recording finalization/reconnect work.
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','state.recorder.requestData()')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','finally {\n            releaseRecordingCapture(state);')
    require('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor','getMediaRecordingState')
    print('PublisherStudio 2.7.5 overlay-safe logging source audit passed.')
except Exception as exc:
    print(f'PublisherStudio 2.7.5 source audit failed: {exc}',file=sys.stderr)
    raise SystemExit(1)
