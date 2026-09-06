#!/usr/bin/env python3
from pathlib import Path
import json, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
errors=[]

def text(rel):
    p=ROOT/rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig',errors='replace')

def req(rel,needle):
    if needle not in text(rel): errors.append(f'{rel} missing: {needle}')

def forbid(rel,needle):
    if needle in text(rel): errors.append(f'{rel} contains forbidden: {needle}')

version=(3,3,1)
if any(x>9 for x in version[1:]): errors.append('version violates one-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.3.1</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        data=json.loads(text(rel))
        if data.get('version')!='3.3.1': errors.append(f'{rel} top-level version is not 3.3.1')
        if rel.endswith('package-lock.json') and data.get('packages',{}).get('',{}).get('version')!='3.3.1': errors.append('package-lock.json root package version is not 3.3.1')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
try:
    data=json.loads(text('docs/docfx.json'))
    if data.get('build',{}).get('globalMetadata',{}).get('publisherstudioVersion')!='3.3.1': errors.append('docs/docfx.json publisherstudioVersion is not 3.3.1')
except Exception as exc: errors.append(f'docs/docfx.json JSON parse failed: {exc}')
req('docs/pdf/toc.yml','PublisherStudio-3.3.1.pdf')
req('docs/index.md','**Version 3.3.1**')
req('RELEASE.md','# PublisherStudio 3.3.1')
req('CHANGELOG-v3.3.1-RELEASE-RESUME-PDF-SIZE-CONTROL.md','Version advanced from 3.3.0 to 3.3.1')
req('VALIDATION-v3.3.1-source.md','# PublisherStudio 3.3.1 source validation')

build=text('Build-Release.ps1')
for marker in ('[switch]$AllowUnsignedMacPackages','build/Initialize-MacReleaseTrust.ps1',"-ProductName 'PublisherStudio'",'-AllowUnsignedMacPackages:$AllowUnsignedMacPackages'):
    if marker not in build: errors.append(f'Build-Release.ps1 missing: {marker}')
trust=text('build/Initialize-MacReleaseTrust.ps1')
for marker in ("$env:MACOS_REQUIRE_NOTARIZATION = '1'","'future2-notary'",'notarytool history --keychain-profile','Developer ID Application:','Developer ID Installer:','Apple Developer ID certificates are not a Windows trust identity'):
    if marker not in trust: errors.append(f'build/Initialize-MacReleaseTrust.ps1 missing: {marker}')
native=text('build/NativeReleasePackaging.ps1')
for marker in (
    'sysctl -n hw.optional.arm64','sysctl -n sysctl.proc_translated','PUBLISHERSTUDIO_NATIVE_REEXEC','native-architecture-manifest.txt',
    'Assert-MacBundleArchitecture $app $Rid','com.apple.security.cs.allow-jit','$nestedCodeBundles = @(','Developer ID signed and verified',
    'function Sign-MacDiskImage','Sign-MacDiskImage $Destination','--type open',"'context:primary-signature'",'--type install','notarytool submit','stapler staple','stapler validate','--check-signature','Signed, notarized, stapled, and validated macOS'):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing: {marker}')

req('build/Assert-SourcePackagePrerequisites.ps1','build/Initialize-MacReleaseTrust.ps1')
req('build/Ensure-ReleasePackagingPackage.ps1','[string]$Version = "1.0.1"')

# 3.6.9/3.3.1 release-size and resume invariants.
build=text('Build-Release.ps1')
for marker in (
    '[string]$DocumentationCacheRoot', '[string]$ReleaseOutputRoot', '[switch]$ForceRebuildArtifacts',
    "$mode = 'Full'", "$selfContained = 'true'", 'FUTURE2_DOCUMENTATION_CACHE_ROOT', 'FUTURE2_RELEASE_OUTPUT_ROOT', 'Clear-RepositoryReleaseBuildState', '-ProbeExistingArtifactsOnly', 'Test-ExistingReleaseBundleComplete', 'Move-OrReuseReleaseFile'
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing release-size/resume marker: {marker}')
if "foreach ($mode in @('Full','Light'))" in build: errors.append('Build-Release.ps1 still emits the removed Light Unix/macOS lane')
docs=text('build/Build-Documentation.ps1')
for marker in ('FUTURE2_DOCUMENTATION_PDF_MAX_BYTES','Ensure-', 'ghostscript-screen-optimized','browserPdfTimeoutMilliseconds','maximumSanePdfBytes','268435456L','cached-validated-pdf','brewPath install ghostscript'):
    if marker not in docs: errors.append(f'build/Build-Documentation.ps1 missing PDF-size marker: {marker}')
native=text('build/NativeReleasePackaging.ps1')
for marker in ('Test-MacDistributionArtifactReady','Reusing already signed, notarized, stapled','brew install rpm','[switch]$ForceRebuildArtifacts','[switch]$ProbeExistingArtifactsOnly','The complete $Rid Full macOS release already exists','notary-state.json','MACOS_NOTARY_WAIT_TIMEOUT','notarytool wait','Resuming Apple notarization submission'):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing resume/RPM marker: {marker}')

if 'Copy-PublisherStudioRuntimeDocumentation' not in build: errors.append('Publisher runtime documentation is not HTML-only')
forbid('Build-Release.ps1','signtool')
forbid('build/NativeReleasePackaging.ps1','osslsigncode')

# Preserve the 3.2.7 service fix.
service='src/PublisherStudio.Web/Services/Configuration/ApplicationPathService.cs'
req(service,'private string KnownFolderOrFallback(Environment.SpecialFolder folder, string fallback)')
req(service,'logger.LogTrace($"Entering ApplicationPathService.KnownFolderOrFallback for {folder}.")')
req(service,'logger.LogError(exception, $"ApplicationPathService.KnownFolderOrFallback failed for {folder}: {exception.Message}")')
forbid(service,'private static string KnownFolderOrFallback')

for rel in ('Components/Pages/Editor.razor','Components/Pages/Help.razor','Components/Pages/Localization.razor','Components/Pages/OrganicPlugins.razor'):
    req('src/PublisherStudio.Web/'+rel,'@rendermode InteractiveServer')
count=0
for p in (ROOT/'src/PublisherStudio.Web').rglob('*.razor'):
    count += p.read_text(encoding='utf-8-sig',errors='replace').count('@rendermode InteractiveServer')
if count != 4: errors.append(f'InteractiveServer occurrence count changed: expected 4, found {count}')

for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts: errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')

if errors:
    print('PublisherStudio 3.3.1 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('PublisherStudio 3.3.1 source audit passed.')
