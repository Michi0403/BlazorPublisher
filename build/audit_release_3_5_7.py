from pathlib import Path
import sys
root = Path(__file__).resolve().parents[1]
fail=[]
def need(path, text, label=None):
    s=(root/path).read_text(encoding='utf-8')
    if text not in s: fail.append(label or f"{path}: missing {text!r}")
    return s
web=need(Path('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), '<Version>3.5.7</Version>')
need(Path('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), '<Version>3.5.7</Version>')
need(Path('CHANGELOG-v3.5.7-RENDER-AFFINITY-LOGGING-INSTALLER-REPAIR.md'), '3.5.7')
need(Path('VALIDATION-v3.5.7-source.md'), '3.5.7')
story=need(Path('src/PublisherStudio.Web/Components/Editor/StoryEditor.razor'), 'if (!Visible || _storyLayoutAttached) return;', 'StoryEditor: missing one-shot layout guard')
if story.count('_storyLayoutAttached = true;') != 1: fail.append('StoryEditor: layout attachment must become true exactly once in the successful bridge path')
need(Path('src/PublisherStudio.Web/Components/Editor/StoryEditor.razor'), '_storyLayoutAttached = false;', 'StoryEditor: missing reset path')
logger=need(Path('src/PublisherStudio.Web/Services/Logging/FileLogger.cs'), 'internal sealed class FileLoggerSharedSink')
if logger.count('new Thread(') != 1: fail.append('FileLogger: expected exactly one provider-owned writer thread construction')
need(Path('src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs'), 'sink = new FileLoggerSharedSink(options);')
need(Path('src/PublisherStudio.Web/Diagnostics/DebugFirstChanceExceptionLoggingHostedService.cs'), 'PublisherStudio.Services.Logging.FileLoggerSharedSink')
program=need(Path('src/PublisherStudio.InstallerConsole/Program.cs'), 'ExtractZipWithFallback(zipPath, targetPath, logger);')
need(Path('src/PublisherStudio.InstallerConsole/Program.cs'), 'ExtractZipWithFallback(setupZipPath, targetPath, logger);')
for forbidden in (
    'InstallReleaseArchivesTransactionally(',
    'ValidateStagedAssemblyVersion(',
    'ReadStagedReleaseIdentity(',
    'rollback-capable transaction',
    'transactional wrapper replacement',
    'whole-directory replacement workflow',
):
    if forbidden in program: fail.append(f'Installer: forbidden unapproved transactional deployment dialect remains: {forbidden}')
audit=need(Path('build/audit_async_continuations.py'), 'Renderer/circuit-affine component continuations must use ConfigureAwait(true).')
build=need(Path('Build-Release.ps1'), 'Preflighting the PublisherStudio installer compile before expensive documentation and native packaging...')
pre=build.find('Preflighting the PublisherStudio installer compile before expensive documentation and native packaging...')
docs=build.find('Prepare-PublisherStudioDocumentation', pre)
if pre < 0 or docs < 0 or pre > docs: fail.append('Build-Release.ps1: setup compile preflight must occur before documentation generation')
if fail:
    print('PublisherStudio 3.5.7 release audit FAILED:')
    print('\n'.join(' - '+x for x in fail)); sys.exit(1)
print('PublisherStudio 3.5.7 release audit passed: versioning, render-affinity guard, one-shot Story layout, shared file sink, maintained overlay installer contract, and early installer compile preflight are present.')
