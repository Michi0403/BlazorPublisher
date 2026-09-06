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
version=(3,4,3)
if version[1]>9 or version[2]>9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.4.3</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        d=json.loads(read(rel))
        if d.get('version')!='3.4.3': errors.append(f'{rel} version != 3.4.3')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('publisherstudioVersion')!='3.4.3': errors.append('docs/docfx.json publisherstudioVersion != 3.4.3')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.4.3**'),('docs/pdf/toc.yml','PublisherStudio-3.4.3.pdf'),('RELEASE.md','# PublisherStudio 3.4.3'),
    ('CHANGELOG-v3.4.3-NOTARY-SUBMIT-STATUS-REPAIR.md','Notary submit status repair'),('VALIDATION-v3.4.3-source.md','# PublisherStudio 3.4.3 source validation')): req(rel,mark)
docs=read('build/Build-Documentation.ps1')
for marker in ("$tail.IndexOf('%%EOF', [StringComparison]::Ordinal) -ge 0",'function Set-PortableProcessArguments',"GetProperty('ArgumentList')",'function Stop-PortableProcessTree',"GetMethod('Kill', [Type[]]@([bool]))",'Invoke-PublisherStudioChunkedBrowserPdf','Reusing durable documentation PDF chunk','html-browser-chunked'):
    if marker not in docs: errors.append(f'Build-Documentation missing: {marker}')
native=read('build/NativeReleasePackaging.ps1')
for marker in ('function Get-RelativePathPortable',"GetMethod('GetRelativePath'",'function Invoke-MacNotaryToolWithCredentialRecovery','Save-MacNotaryState -ArtifactPath $ArtifactPath -SubmissionId $submissionId'):
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

# Notarization orchestration must not reintroduce blocking/redundant probes.
build_release=read('Build-Release.ps1')
if build_release.count("build/Initialize-MacReleaseTrust.ps1") != 1: errors.append('Build-Release must invoke Initialize-MacReleaseTrust exactly once')
trust=read('build/Initialize-MacReleaseTrust.ps1')
if 'Read-Host' in trust: errors.append('Initialize-MacReleaseTrust must not block on Read-Host')
if "notarytool history @profileArguments" not in trust: errors.append('startup trust probe must use xcrun notarytool history profile arguments')
native=read('build/NativeReleasePackaging.ps1')
if 'Read-Host' in native: errors.append('NativeReleasePackaging notarization must not block on Read-Host')
for marker in ('MACOS_NOTARY_KEYCHAIN_PATH','Get-MacNotaryCredentialRetrySeconds','Invoke-MacNotaryToolWithCredentialRecovery','retry automatically'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing notary resilience marker: {marker}')
if 'Assert-MacNotaryCredentialsUsable' in native: errors.append('redundant pre-submit notary history assertion must stay removed')

# Regression guard for non-waiting submit responses that omit status.
native=read('build/NativeReleasePackaging.ps1')
for marker in (
    'function Get-MacNotaryObjectPropertyText',
    "Get-MacNotaryObjectPropertyText -InputObject $submit -PropertyName 'id'",
    "Get-MacNotaryObjectPropertyText -InputObject $info -PropertyName 'status'",
    'Apple notarytool info returned valid JSON without a status',
    'Wait-MacNotarySubmission -ArtifactPath $ArtifactPath -SubmissionId $submissionId -NotaryArguments $notaryArgs'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing submit/status regression marker: {marker}')
for forbidden in ('$submit.status','$info.status','$state.submissionId','$state.artifactSha256'):
    if forbidden in native: errors.append(f'NativeReleasePackaging reintroduced StrictMode-unsafe optional property access: {forbidden}')
pages=ROOT/'src/PublisherStudio.Web/Components/Pages'
for p in pages.rglob('*.razor'):
    t=p.read_text(encoding='utf-8-sig',errors='replace')
    if '@page' in t and p.name!='Error.razor' and '@rendermode InteractiveServer' not in t: errors.append(f'routed page lost InteractiveServer: {p.relative_to(ROOT).as_posix()}')
if (ROOT/'src/LocalGPT.ReleasePackaging').exists(): errors.append('PublisherStudio must not duplicate LocalGPT.ReleasePackaging source')
for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts: errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')
if errors:
    print('PublisherStudio 3.4.3 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('PublisherStudio 3.4.3 source audit passed.')
