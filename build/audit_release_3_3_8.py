#!/usr/bin/env python3
from pathlib import Path
import json, re, xml.etree.ElementTree as ET

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

version=(3,3,8)
if any(x>9 for x in version[1:]): errors.append('version violates one-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.3.8</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        data=json.loads(text(rel))
        if data.get('version')!='3.3.8': errors.append(f'{rel} top-level version is not 3.3.8')
        if rel.endswith('package-lock.json') and data.get('packages',{}).get('',{}).get('version')!='3.3.8': errors.append('package-lock.json root package version is not 3.3.8')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
try:
    data=json.loads(text('docs/docfx.json'))
    if data.get('build',{}).get('globalMetadata',{}).get('publisherstudioVersion')!='3.3.8': errors.append('docs/docfx.json publisherstudioVersion is not 3.3.8')
except Exception as exc: errors.append(f'docs/docfx.json JSON parse failed: {exc}')
req('docs/pdf/toc.yml','PublisherStudio-3.3.8.pdf')
req('docs/index.md','**Version 3.3.8**')
req('RELEASE.md','# PublisherStudio 3.3.8')
req('CHANGELOG-v3.3.8-CHUNKED-PDF-RELEASE-VALIDATION.md','html-browser-chunked')
req('VALIDATION-v3.3.8-source.md','# PublisherStudio 3.3.8 source validation')

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
req('build/Ensure-ReleasePackagingPackage.ps1','[string]$Version = "1.0.2"')

# Release-size and resume invariants preserved from the prior release.
build=text('Build-Release.ps1')
for marker in (
    '[string]$DocumentationCacheRoot', '[string]$ReleaseOutputRoot', '[switch]$ForceRebuildArtifacts',
    "$mode = 'Full'", "$selfContained = 'true'", 'FUTURE2_DOCUMENTATION_CACHE_ROOT', 'FUTURE2_RELEASE_OUTPUT_ROOT', 'Clear-RepositoryReleaseBuildState', '-ProbeExistingArtifactsOnly', 'Test-ExistingReleaseBundleComplete', 'Move-OrReuseReleaseFile'
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing release-size/resume marker: {marker}')
if "foreach ($mode in @('Full','Light'))" in build: errors.append('Build-Release.ps1 still emits the removed Light Unix/macOS lane')
docs=text('build/Build-Documentation.ps1')
for marker in ('Resolve-PublisherStudioHomebrew','/opt/homebrew/bin/brew','/usr/local/bin/brew','DisablePdfToolProvisioning','brewPath install ghostscript','FUTURE2_DOCUMENTATION_PDF_MAX_BYTES','Ensure-', 'ghostscript-screen-optimized','browserPdfTimeoutMilliseconds','maximumSanePdfBytes','268435456L','cached-validated-pdf','brewPath install ghostscript','runtimePdfPublished = Test-Path -LiteralPath (Join-Path $publishRoot $pdfName)'):
    if marker not in docs: errors.append(f'build/Build-Documentation.ps1 missing PDF-size marker: {marker}')
native=text('build/NativeReleasePackaging.ps1')
if 'notarytool wait' in native: errors.append('build/NativeReleasePackaging.ps1 uses unsupported notarytool wait subcommand; resume must poll notarytool info')
for marker in ('Test-MacDistributionArtifactReady','Reusing already signed, notarized, stapled','brew install rpm','[switch]$ForceRebuildArtifacts','[switch]$ProbeExistingArtifactsOnly','The complete $Rid Full macOS release already exists','notary-state.json','MACOS_NOTARY_WAIT_TIMEOUT','MACOS_NOTARY_POLL_INTERVAL_SECONDS','Start-Sleep -Seconds $sleepSeconds','Resuming Apple notarization submission'):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing resume/RPM marker: {marker}')

if 'Copy-PublisherStudioRuntimeDocumentation' not in build: errors.append('Publisher runtime documentation copy helper is missing')

# The compressed handbook must remain embedded in runtime help and portable archives.
for marker in (
    'Embedded PublisherStudio documentation PDF is missing after runtime documentation copy',
    'runtimePdfPublished -NotePropertyValue $true',
    'pdfAvailable -NotePropertyValue $true',
    'compressed embedded PDF documentation',
    'Runtime release archive must contain exactly the current compressed embedded PublisherStudio PDF',
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing embedded-PDF marker: {marker}')
for forbidden in ('without duplicating the standalone release PDF','Runtime HTML documentation must not duplicate the standalone release PDF','pdfAvailable=false when the standalone PDF is not embedded'):
    if forbidden in build: errors.append(f'Build-Release.ps1 contains obsolete HTML-only runtime-PDF policy: {forbidden}')
if 'Remove-Item -LiteralPath $sourcePdfPath -Force' in docs:
    errors.append('build/Build-Documentation.ps1 still deletes the generated PDF from the runtime help-docs tree')
for marker in ('Embedded PublisherStudio documentation PDF was not published into the runtime help-docs tree','runtimePdfPublished -NotePropertyValue $true','pdfAvailable -NotePropertyValue $true'):
    if marker not in docs: errors.append(f'build/Build-Documentation.ps1 missing embedded-PDF marker: {marker}')


# Adaptive managed PDF pipeline with LocalGPT-owned package consumption only.
if (ROOT / 'src' / 'LocalGPT.ReleasePackaging').exists():
    errors.append('PublisherStudio contains duplicate LocalGPT.ReleasePackaging source; LocalGPT must remain the sole source owner')
for marker in ('[string]$PackagingTool','FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_PAGES','Invoke-PublisherStudioChunkedBrowserPdf','-FrontMatterOnly','-SuppressFrontMatter',"$mergeArguments.Add('pdf-merge')",'Find-PublisherStudioQpdf',"$managedArguments.Add('pdf-optimize')",'release-packaging-adaptive','html-browser-chunked'):
    if marker not in docs: errors.append(f'build/Build-Documentation.ps1 missing adaptive-PDF marker: {marker}')
if 'Remove-PublisherStudioTemporaryPath -Path $destinationDirectory' in docs:
    errors.append('build/Build-Documentation.ps1 deletes the shared chunk directory while constructing a chunk; earlier browser PDF chunks would be lost before merge')
for marker in ('[string]$ReleasePackagingVersion = "1.0.2"','-PackagingTool $releasePackagingTool'):
    if marker not in build: errors.append(f'Build-Release.ps1 missing managed-PDF packaging marker: {marker}')
ensure=text('build/Ensure-ReleasePackagingPackage.ps1')
for marker in (
    '[string]$Version = "1.0.2"',
    'Test-ReleasePackagingPackage',
    'LOCALGPT_REPOSITORY',
    "Join-Path (Split-Path -Parent $repositoryRoot) 'LocalGPT'",
    "LocalGPT', 'NuGet', $packageName",
    'https://github.com/Michi0403/LocalGPT/releases/latest/download/$packageName',
    'dotnet tool install LocalGPT.ReleasePackaging',
    'https://api.nuget.org/v3/index.json',
    'PublisherStudio intentionally does not carry or compile LocalGPT.ReleasePackaging source.',
):
    if marker not in ensure: errors.append(f'build/Ensure-ReleasePackagingPackage.ps1 missing package-consumer marker: {marker}')
for forbidden_marker in ('dotnet pack', 'vendored source fallback', "src', 'LocalGPT.ReleasePackaging", 'src/LocalGPT.ReleasePackaging'):
    if forbidden_marker in ensure: errors.append(f'build/Ensure-ReleasePackagingPackage.ps1 contains forbidden source-owner behavior: {forbidden_marker}')
req('packages/README.md','PublisherStudio does not own duplicate source')

# 3.3.8 regression: PublisherStudio generates the same adaptive chunked browser PDF mode and
# must accept it while retaining page/API completeness and accessibility validation.
for marker in (
    '$browserBackedPdfModes = @("html-browser-print", "html-browser-print-compatibility", "html-browser-chunked")',
    '[string]$status.pdfMode -in $browserBackedPdfModes',
    'The PublisherStudio documentation PDF omitted generated API pages.',
    'unexpected PDF accessibility mode',
    'cached-validated-pdf',
    '@("tagged-pdf-required", "html-accessibility-fallback")',
):
    if marker not in build: errors.append(f'Build-Release.ps1 missing chunked-PDF validator marker: {marker}')
req('build/Build-Documentation.ps1', 'if ($pdfMode -in @("html-browser-print", "html-browser-print-compatibility", "html-browser-chunked")) {')
render_guard=text('build/Assert-InteractiveServerRenderModes.ps1')
for marker in (
    "Get-ChildItem -LiteralPath $pagesRoot -Recurse -File -Filter '*.razor'",
    "if ($relative -eq 'Components/Pages/Error.razor')",
    'is a routed application page but is not registered as a prerendered InteractiveServer page.',
):
    if marker not in render_guard: errors.append(f'PublisherStudio render-mode audit missing routed-page guard: {marker}')

# PublisherStudio must fail malformed packaging scripts before expensive release preparation.
req('Build-Release.ps1', "build/Assert-PowerShellCompatibility.ps1")
compat=text('build/Assert-PowerShellCompatibility.ps1')
for marker in ('System.Management.Automation.Language.Parser','has a PowerShell parser error'):
    if marker not in compat: errors.append(f'build/Assert-PowerShellCompatibility.ps1 missing parser-preflight marker: {marker}')
for marker in ('${SubmissionId}:','${ArtifactPath}:'):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing delimited interpolation marker: {marker}')
suspicious_colon = re.compile(r'\$([A-Za-z_][A-Za-z0-9_]*):')
allowed_scopes = {'script','env','global','local','private','using'}
for ps_path in list(ROOT.rglob('*.ps1')) + list(ROOT.rglob('*.psm1')):
    rel = ps_path.relative_to(ROOT).as_posix()
    if re.search(r'(^|/)(\.git|\.vs|artifacts|bin|obj|packages|node_modules)(/|$)', rel):
        continue
    value = ps_path.read_text(encoding='utf-8-sig', errors='replace')
    for line_number, line in enumerate(value.splitlines(), 1):
        for match in suspicious_colon.finditer(line):
            if match.group(1).lower() not in allowed_scopes:
                errors.append(f'{rel}:{line_number} has suspicious unbraced variable-plus-colon interpolation: {match.group(0)}')

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
    print('PublisherStudio 3.3.8 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('PublisherStudio 3.3.8 source audit passed.')
