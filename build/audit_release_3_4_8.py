#!/usr/bin/env python3
from pathlib import Path
import json,re,sys,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]; errors=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file(): errors.append(f'missing file: {rel}'); return ''
    return p.read_text(encoding='utf-8-sig',errors='replace')
def req(rel,m):
    if m not in read(rel): errors.append(f'{rel} missing marker: {m}')
def forbid_all(pattern,message):
    rx=re.compile(pattern,re.I|re.M)
    for p in ROOT.rglob('*'):
        if p.suffix.lower() not in ('.ps1','.psm1') or not p.is_file(): continue
        rel=p.relative_to(ROOT).as_posix()
        if re.search(r'(^|/)(bin|obj|artifacts|packages|node_modules)(/|$)',rel): continue
        text=p.read_text(encoding='utf-8-sig',errors='replace')
        for m in rx.finditer(text): errors.append(f'{rel}:{text.count(chr(10),0,m.start())+1} {message}')
version=(3, 4, 8)
if version[1]>9 or version[2]>9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.4.8</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        d=json.loads(read(rel))
        if d.get('version')!='3.4.8': errors.append(f'{rel} version != 3.4.8')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('publisherstudioVersion')!='3.4.8': errors.append('docs/docfx.json publisherstudioVersion != 3.4.8')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.4.8**'),('docs/pdf/toc.yml','PublisherStudio-3.4.8.pdf'),('RELEASE.md','# PublisherStudio 3.4.8'),
    ('CHANGELOG-v3.4.8-PATH-LAYOUT-XML-DOCUMENTATION-REPAIR.md','path-layout XML documentation repair'),('VALIDATION-v3.4.8-source.md','# PublisherStudio 3.4.8 source validation')): req(rel,mark)
docs=read('build/Build-Documentation.ps1')
for marker in ("$tail.IndexOf('%%EOF', [StringComparison]::Ordinal) -ge 0",'function Set-PortableProcessArguments',"GetProperty('ArgumentList')",'function Stop-PortableProcessTree',"GetMethod('Kill', [Type[]]@([bool]))",'Invoke-PublisherStudioChunkedBrowserPdf','Reusing durable documentation PDF chunk','html-browser-chunked'):
    if marker not in docs: errors.append(f'Build-Documentation missing: {marker}')
local_app_assignment = '$localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)'
if local_app_assignment not in docs: errors.append('Build-Documentation does not initialize localApplicationData before browser discovery')
else:
    if docs.index(local_app_assignment) > docs.index('function Find-PublisherStudioDocumentationBrowser'):
        errors.append('localApplicationData is initialized after browser discovery instead of before it')
for marker in ('$requiresChunkedBrowserPdf =', 'Refusing the legacy DocFX PDF fallback because it can generate multi-gigabyte PDFs'):
    if marker not in docs: errors.append(f'Build-Documentation missing large-PDF fail-fast marker: {marker}')
native=read('build/NativeReleasePackaging.ps1')
for marker in ('function Get-RelativePathPortable',"GetMethod('GetRelativePath'",'function Invoke-MacNotaryToolWithCredentialRecovery','function Save-MacNotarySubmittedState'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing: {marker}')
compat=read('build/Assert-PowerShellCompatibility.ps1')
for marker in ('$unsupportedContainsPattern','$unsupportedPathRelativePattern','$unsupportedArgumentListPattern','$unsupportedKillTreePattern','Windows PowerShell 5.1 and modern pwsh'):
    if marker not in compat: errors.append(f'compatibility guard missing: {marker}')
forbid_all(r'\.Contains\([^\r\n]*,\s*\[(?:System\.)?StringComparison\]::','uses incompatible String.Contains comparison overload')
forbid_all(r'\[(?:System\.)?IO\.Path\]::GetRelativePath\s*\(','uses direct Path.GetRelativePath')
forbid_all(r'\.ArgumentList(?:\.|\s*=)','uses direct ProcessStartInfo.ArgumentList')
forbid_all(r'\.Kill\(\s*\$true\s*\)','uses direct Process.Kill(true)')
forbid_all(r'ForEach-Object\s+-Parallel\b','uses PowerShell 7-only ForEach-Object -Parallel')
forbid_all(r'ConvertFrom-Json\s+[^\r\n]*-AsHashtable\b','uses PowerShell 6+ ConvertFrom-Json -AsHashtable')
forbid_all(r'\bJoin-String\b','uses PowerShell 6+ Join-String')
forbid_all(r'\bTest-Json\b','uses PowerShell 6+ Test-Json')
forbid_all(r'\$PSStyle\b','uses PowerShell 7.2+ PSStyle')

# Notarization is a per-artifact transaction. `submit` is non-idempotent and must never be
# run through a retry loop. Pending state is written before upload and ambiguous local results
# are reconciled through idempotent Apple history queries.
build_release=read('Build-Release.ps1')
if build_release.count("build/Initialize-MacReleaseTrust.ps1") != 1: errors.append('Build-Release must invoke Initialize-MacReleaseTrust exactly once')
trust=read('build/Initialize-MacReleaseTrust.ps1')
if 'Read-Host' in trust: errors.append('Initialize-MacReleaseTrust must not block on Read-Host')
native=read('build/NativeReleasePackaging.ps1')
for marker in (
    'function Invoke-MacNotaryToolOnce',
    'function Invoke-MacNotaryToolWithCredentialRecovery',
    'function New-MacNotaryPendingState',
    'function Save-MacNotarySubmittedState',
    'function Get-MacNotaryHistoryEntries',
    'function Resolve-MacNotaryPendingSubmission',
    'baselineSubmissionIds',
    "phase = 'submit-pending'",
    'Submitting $artifactName to Apple notary service exactly once for this transaction',
    'no duplicate upload was issued',
    "Get-MacNotaryObjectPropertyText -InputObject $info -PropertyName 'status'",
    'Set-MacNotaryStatePhase -ArtifactPath $ArtifactPath -State $state -Phase complete'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing artifact-local notary marker: {marker}')
if 'Read-Host' in native: errors.append('NativeReleasePackaging notarization must not block on Read-Host')
if 'Assert-MacNotaryCredentialsUsable' in native: errors.append('redundant pre-submit notary assertion must stay removed')
# Exactly one executable submit argument construction exists in the native packager.
submit_lines=[line for line in native.splitlines() if "@('submit',$ArtifactPath)" in line]
if len(submit_lines)!=1: errors.append(f'expected exactly one notary submit construction, found {len(submit_lines)}')
# The submit call itself must use Invoke-MacNotaryToolOnce, while recovery wrapper call sites are
# limited to history/info/log (idempotent operations).
if 'Invoke-MacNotaryToolOnce -Operation "single upload of $artifactName" -Arguments $submitArguments' not in native:
    errors.append('notary submit is not using the one-shot invocation path')
for m in re.finditer(r'Invoke-MacNotaryToolWithCredentialRecovery[^\n]*', native):
    line=m.group(0)
    if line.startswith('Invoke-MacNotaryToolWithCredentialRecovery('):
        continue
    if not any(token in line for token in ('submission-history query','status query for submission','failure-log download')):
        errors.append(f'retry wrapper used outside idempotent notary query: {line.strip()}')
for forbidden in ('$submit.status','$info.status','$state.submissionId','$state.artifactSha256'):
    if forbidden in native: errors.append(f'NativeReleasePackaging reintroduced StrictMode-unsafe optional property access: {forbidden}')
# Completed state is retained as a hash-bound audit/resume record; it must not be deleted after staple.
if re.search(r'(?m)^\s*Remove-MacNotaryState\s+\$ArtifactPath\s*$', native):
    errors.append('completed artifact state is still deleted instead of retained')
for marker in ("schema = 3", 'finalArtifactSha256', '$stateBeforeStaple = Get-MacNotaryState $ArtifactPath', 'outside the recorded submitted/stapled hashes'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing stapled-state marker: {marker}')
# FFmpeg path policy stays extensible: common locator owns precedence, OS-specific candidates stay in platform services.
locator=read('src/PublisherStudio.Web/Services/Streaming/Encoding/FfmpegLocator.cs')
for marker_text in ('configuredPath','AppContext.BaseDirectory','KnownInstallLocations()','Environment.GetEnvironmentVariable("PATH")'):
    if marker_text not in locator: errors.append(f'FfmpegLocator lost path-precedence marker: {marker_text}')
if 'OperatingSystem.Is' in locator or 'RuntimeInformation.IsOSPlatform' in locator:
    errors.append('FfmpegLocator leaked OS branching back into the common locator')
platform_runtime=read('src/PublisherStudio.Web/Services/PublisherPlatformRuntimeServices.cs')
for marker_text in ('Path.Combine(home, ".local", "bin", "ffmpeg")','Path.Combine(home, "bin", "ffmpeg")','Path.Combine(home, ".nix-profile", "bin", "ffmpeg")','/home/linuxbrew/.linuxbrew/bin/ffmpeg','FfmpegUnixInstallPaths'):
    if marker_text not in platform_runtime: errors.append(f'Unix FFmpeg platform discovery missing: {marker_text}')
provisioner=read('src/PublisherStudio.InstallerConsole/FfmpegProvisioner.cs')
for marker_text in ('Path.Combine(home, ".local", "bin", "ffmpeg")','Path.Combine(home, ".nix-profile", "bin", "ffmpeg")','/home/linuxbrew/.linuxbrew/bin/ffmpeg'):
    if marker_text not in provisioner: errors.append(f'InstallerConsole FFmpeg discovery missing runtime-parity candidate: {marker_text}')
media=read('src/PublisherStudio.Web/Services/MediaConversion/MediaConversionService.cs')
for marker_text in ('PublisherStudio:FFmpegPath','PUBLISHERSTUDIO_FFMPEG','tools/ffmpeg','~/.local/bin','Homebrew/Linuxbrew/Nix'):
    if marker_text not in media: errors.append(f'Media conversion FFmpeg guidance missing: {marker_text}')

pages=ROOT/'src/PublisherStudio.Web/Components/Pages'
for p in pages.rglob('*.razor'):
    t=p.read_text(encoding='utf-8-sig',errors='replace')
    if '@page' in t and p.name!='Error.razor' and '@rendermode InteractiveServer' not in t: errors.append(f'routed page lost InteractiveServer: {p.relative_to(ROOT).as_posix()}')
if (ROOT/'src/LocalGPT.ReleasePackaging').exists(): errors.append('PublisherStudio must not duplicate LocalGPT.ReleasePackaging source')
for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts: errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')
# 3.4.8 application-owned storage contract. PublisherStudio mutable state defaults to the current
# user while tool discovery remains allowed to inspect portable/user/system installation locations.
platform_paths=read('src/PublisherStudio.Web/Program.cs')
for marker_text in (
    'internal static class PublisherApplicationDataPaths',
    'Environment.GetEnvironmentVariable("LOCALAPPDATA")',
    'Path.Combine(userProfile, "AppData", "Local")',
    'Path.Combine(userProfile, "Library", "Application Support")',
    'Environment.GetEnvironmentVariable("XDG_DATA_HOME")',
    'Path.Combine(userProfile, ".local", "share")',
    'public static string ResolveUserRoot() => Path.Combine(ResolveUserDataBase(), ProductName)',
    '/Library/Application Support/PublisherStudio', '/var/lib/PublisherStudio', '/usr/share/PublisherStudio', '/opt/PublisherStudio'):
    if marker_text not in platform_paths: errors.append(f'PublisherStudio application-data policy missing: {marker_text}')
if platform_paths.index('Environment.GetEnvironmentVariable("XDG_DATA_HOME")') > platform_paths.index('var linuxLocal = Environment.GetFolderPath'):
    errors.append('Linux XDG_DATA_HOME must be evaluated before the generic LocalApplicationData fallback')
path_service=read('src/PublisherStudio.Web/Services/Configuration/ApplicationPathService.cs')
for marker_text in (
    'Path.Combine(userDataRoot, "Images")','Path.Combine(userDataRoot, "Video")','Path.Combine(userDataRoot, "Audio")',
    'Path.Combine(userDataRoot, "Documents")','Path.Combine(userDataRoot, "Exports")','Path.Combine(userDataRoot, "OpenSCAD")','Path.Combine(userDataRoot, "Projects")',
    'Configuration", "path-layout.json','FirstBootDetected = !File.Exists(reportFile)','EnsureAndDocumentLayout'):
    if marker_text not in path_service: errors.append(f'PublisherStudio application path contract missing: {marker_text}')
for forbidden in ('SpecialFolder.MyDocuments','SpecialFolder.MyPictures','SpecialFolder.MyVideos','SpecialFolder.MyMusic'):
    if forbidden in path_service: errors.append(f'PublisherStudio default content escaped into personal shell folders: {forbidden}')
program=read('src/PublisherStudio.Web/Program.cs')
for marker_text in ('ResolveUserPath("Configuration", "appsettings.user.json")','AddJsonFile(userSettingsFile, optional: true','AddEnvironmentVariables()','EnsureAndDocumentLayout()'):
    if marker_text not in program: errors.append(f'PublisherStudio startup path/config marker missing: {marker_text}')
config_controller=read('src/PublisherStudio.Web/Controllers/ConfigurationController.cs')
if '[HttpGet("path-layout")]' not in config_controller or 'paths.GetLayout()' not in config_controller:
    errors.append('PublisherStudio path-layout diagnostics API is missing')
help_page=read('src/PublisherStudio.Web/Components/Pages/Help.razor')
for marker_text in ('PathLayout.UserDataRoot','PathLayout.UserConfigurationFile','PathLayout.LayoutReportFile'):
    if marker_text not in help_page: errors.append(f'PublisherStudio Help path diagnostics missing: {marker_text}')
system_store=read('src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs')
if 'ResolveUserPath("Configuration")' not in system_store or 'system-variables.json' not in system_store:
    errors.append('PublisherStudio system-variable store does not use canonical per-user Configuration root')
# App-owned runtime services should not construct their own LocalApplicationData PublisherStudio root.
allowed_local_appdata={
    'src/PublisherStudio.Web/Services/PublisherPlatformRuntimeServices.cs',
    'src/PublisherStudio.InstallerConsole/FfmpegProvisioner.cs',
    'src/PublisherStudio.InstallerConsole/Program.cs',
    'src/PublisherStudio.Web/Program.cs',
}
for p in (ROOT/'src').rglob('*.cs'):
    rel=p.relative_to(ROOT).as_posix()
    if 'SpecialFolder.LocalApplicationData' in p.read_text(encoding='utf-8-sig', errors='replace') and rel not in allowed_local_appdata:
        errors.append(f'application-owned code bypasses PublisherApplicationDataPaths: {rel}')

if errors:
    print('PublisherStudio 3.4.8 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('PublisherStudio 3.4.8 source audit passed.')
