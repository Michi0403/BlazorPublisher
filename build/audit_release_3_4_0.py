#!/usr/bin/env python3
from pathlib import Path
import json, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
errors=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file(): errors.append(f'missing file: {rel}'); return ''
    return p.read_text(encoding='utf-8-sig',errors='replace')
def req(rel,m):
    if m not in read(rel): errors.append(f'{rel} missing marker: {m}')
version=(3,4,0)
if version[1]>9 or version[2]>9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.4.0</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        d=json.loads(read(rel))
        if d.get('version')!='3.4.0': errors.append(f'{rel} version != 3.4.0')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('publisherstudioVersion')!='3.4.0': errors.append('docs/docfx.json publisherstudioVersion != 3.4.0')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.4.0**'),('docs/pdf/toc.yml','PublisherStudio-3.4.0.pdf'),
    ('RELEASE.md','# PublisherStudio 3.4.0'),('CHANGELOG-v3.4.0-RELEASE-ORCHESTRATION-RESILIENCE.md','3.3.9 to 3.4.0'),
    ('VALIDATION-v3.4.0-source.md','# PublisherStudio 3.4.0 source validation')):
    req(rel,mark)

native=read('build/NativeReleasePackaging.ps1')
for marker in (
    'function Test-MacNotaryCredentialRecoveryRequired','function Wait-MacNotaryCredentialRecovery',
    'function Invoke-MacNotaryToolWithCredentialRecovery','Read-Host "Unlock/approve the macOS Keychain prompt',
    "@('submit',$ArtifactPath)",'Save-MacNotaryState -ArtifactPath $ArtifactPath -SubmissionId $submissionId',
    'Apple upload completed; submission $submissionId was persisted immediately','progress checkpoint only',
    'Continuing to wait; no upload or build work will be repeated','Resuming Apple notarization submission',
    'Signed, notarized, stapled, and validated macOS',
):
    if marker not in native: errors.append(f'NativeReleasePackaging missing: {marker}')
for forbidden in ('notarytool submit $ArtifactPath @notaryArgs --wait --timeout','submit --wait --timeout'):
    if forbidden in native: errors.append(f'NativeReleasePackaging still contains single-process wait regression: {forbidden}')
trust=read('build/Initialize-MacReleaseTrust.ps1')
for marker in ("'future2-notary'",'Test-MacNotaryCredentialRecoveryRequired', 'Test-MacNotaryTransientServiceRecoveryRequired', 'PSNativeCommandUseErrorActionPreference','Wait-MacNotaryCredentialRecovery','Read-Host "Unlock/approve the macOS Keychain prompt','while ($true)'):
    if marker not in trust: errors.append(f'Initialize-MacReleaseTrust missing: {marker}')
build=read('Build-Release.ps1')
for marker in ('build/Initialize-MacReleaseTrust.ps1',"-ProductName 'PublisherStudio'",'-SelectedRuntimes @($Rid)','-AllowUnsignedMacPackages:$AllowUnsignedMacPackages','[string]$ReleasePackagingVersion = "1.0.2"'):
    if marker not in build: errors.append(f'Build-Release missing: {marker}')
docs=read('build/Build-Documentation.ps1')
for marker in ('Invoke-PublisherStudioChunkedBrowserPdf','[Parameter(Mandatory)][string]$ChunkCacheRoot',"Join-Path $documentationCacheEntryRoot 'browser-pdf-chunks'",'Reusing durable documentation PDF chunk',"$tail.Contains('%%EOF', [StringComparison]::Ordinal)",'Completed documentation PDF chunk','Durable chunks were retained for the next attempt',"$mergeArguments.Add('pdf-merge')",'html-browser-chunked','cached-validated-pdf','Find-PublisherStudioQpdf','Find-PublisherStudioGhostscript'):
    if marker not in docs: errors.append(f'Build-Documentation missing: {marker}')
if (ROOT/'src/LocalGPT.ReleasePackaging').exists(): errors.append('PublisherStudio must not duplicate LocalGPT.ReleasePackaging source')
pages_root=ROOT/'src/PublisherStudio.Web/Components/Pages'
for p in pages_root.rglob('*.razor'):
    text=p.read_text(encoding='utf-8-sig',errors='replace')
    if '@page' not in text: continue
    rel=p.relative_to(ROOT).as_posix()
    if p.name=='Error.razor': continue
    if '@rendermode InteractiveServer' not in text: errors.append(f'routed page lost InteractiveServer: {rel}')
for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts: errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')
if errors:
    print('PublisherStudio 3.4.0 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('PublisherStudio 3.4.0 source audit passed.')
