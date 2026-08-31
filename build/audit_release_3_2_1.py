#!/usr/bin/env python3
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
errors = []

def text(rel):
    p = ROOT / rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig', errors='replace')

def req(rel, needle, msg=None):
    if needle not in text(rel):
        errors.append(msg or f'{rel} missing: {needle}')

def forbid(rel, needle, msg=None):
    if needle in text(rel):
        errors.append(msg or f'{rel} contains forbidden: {needle}')

for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel, '<Version>3.2.1</Version>')
req('src/PublisherStudio.Web/package.json', '"version": "3.2.1"')
req('src/PublisherStudio.Web/package-lock.json', '"version": "3.2.1"')
req('docs/docfx.json', '"publisherstudioVersion": "3.2.1"')
req('docs/pdf/toc.yml', 'PublisherStudio-3.2.1.pdf')
req('RELEASE.md', 'CHANGELOG-v3.2.1-MACOS-DOCUMENTATION-PDF-RENDER-RECOVERY.md')
req('RELEASE.md', 'VALIDATION-v3.2.1-source.md')
text('CHANGELOG-v3.2.1-MACOS-DOCUMENTATION-PDF-RENDER-RECOVERY.md')
text('VALIDATION-v3.2.1-source.md')


# macOS PDF recovery: validated browser candidates, compatibility renderer, and bounded fallback timeout.
doc = text('build/Build-Documentation.ps1')
for marker in (
    '$isMacOsHost',
    '$pdfTimeoutMilliseconds = if ($isMacOsHost) { 300000 } else { 1800000 }',
    'Name = "tagged"',
    'Name = "compatibility"',
    'html-browser-print-compatibility',
    'html-accessibility-fallback',
    'Test-PublisherStudioCompletePdf -Path $PdfPath -MinimumBytes $MinimumBytes',
    '$configuredPdfTimeout -gt 0',
    'pdfAccessibilityMode = $pdfAccessibilityMode',
):
    if marker not in doc:
        errors.append(f'build/Build-Documentation.ps1 missing macOS PDF recovery marker: {marker}')
release_pdf = text('Build-Release.ps1')
for marker in ('html-browser-print-compatibility', '$status.pdfMode -like "html-browser-print*"'):
    if marker not in release_pdf:
        errors.append(f'Build-Release.ps1 missing compatibility PDF validation marker: {marker}')

# Shared packaging remains LocalGPT-owned/local-first.
if (ROOT / 'src/LocalGPT.ReleasePackaging').exists():
    errors.append('duplicate LocalGPT.ReleasePackaging source present')
if (ROOT / 'build/Publish-ReleasePackagingPackage.ps1').exists():
    errors.append('duplicate local packaging publisher present')
req('build/Ensure-ReleasePackagingPackage.ps1', '[string]$Version = "1.0.1"')
forbid('build/Ensure-ReleasePackagingPackage.ps1', 'https://github.com/Michi0403/LocalGPT/releases/latest/download/$packageName')

build = text('Build-Release.ps1')
for marker in (
    '[ValidateSet("Auto", "Off", "Require")]',
    '[string]$WslLinux = "Auto"',
    '[switch]$ProvisionWslBuildTools',
    '[switch]$WslChildBuild',
    '[switch]$SkipReleaseBundle',
    '[switch]$UsePreparedClientAssets',
    "Ready WSL Linux backend '$wslResolvedDistribution' will build:",
    "@('linux-x64','linux-arm64')",
    "Continuing with the normal Windows release only.",
    "build/Invoke-WslLinuxRelease.ps1",
    '-ReleasePackagingPackagePath $releasePackagingPackage',
    '-SkipNodeRuntime:($WslChildBuild -and $UsePreparedClientAssets -and -not [string]::IsNullOrWhiteSpace($PreparedDocumentationRoot))',
):
    if marker not in build:
        errors.append(f'Build-Release.ps1 missing WSL release marker: {marker}')

preflight = text('build/Assert-SourcePackagePrerequisites.ps1')
for marker in (
    '[switch]$SkipNodeRuntime',
    'if ($SkipNodeRuntime)',
    'reuses parent-prepared browser assets and documentation',
):
    if marker not in preflight:
        errors.append(f'Assert-SourcePackagePrerequisites.ps1 missing delegated-node marker: {marker}')

for rel in (
    'Setup-WslLinuxBuild.ps1', 'Setup-WslLinuxBuild.cmd',
    'build/WslRelease.Common.ps1', 'build/Invoke-WslLinuxRelease.ps1',
    'build/wsl/Invoke-LinuxRelease.sh', 'build/wsl/Provision-WslLinuxBuild.sh',
    'docs/articles/wsl-linux-release.md',
):
    text(rel)

common = text('build/WslRelease.Common.ps1')
for marker in (
    'Resolve-WslReleaseDistribution', 'docker-desktop', "printf 'wsl2=0", 
    'DevExpress_License/w', 'DevExpress_LicensePath/pw',
    'WSL2 (convert the distro with wsl.exe --set-version <name> 2)',
):
    if marker not in common:
        errors.append(f'WslRelease.Common.ps1 missing marker: {marker}')
for forbidden in ('DevExpress_License/u', 'DevExpress_LicensePath/pu'):
    if forbidden in common:
        errors.append(f'WslRelease.Common.ps1 contains wrong-direction WSLENV bridge: {forbidden}')

invoke = text('build/Invoke-WslLinuxRelease.ps1')
for marker in (
    "WSL distribution '$distro' is not release-ready",
    "Setup-WslLinuxBuild.ps1 -Provision",
    "foreach ($mode in @('full','light'))",
    "foreach ($extension in @('.tar.gz','.deb'))",
    '--release-packaging-package',
    '--terminate $distro',
):
    if marker not in invoke:
        errors.append(f'Invoke-WslLinuxRelease.ps1 missing marker: {marker}')

child = text('build/wsl/Invoke-LinuxRelease.sh')
for marker in (
    'mktemp -d "$cache_parent/wsl-release-XXXXXXXX"',
    "--exclude='./**/node_modules'",
    '-WslChildBuild', '-SkipReleaseBundle', '-PreparedDocumentationRoot "$docs"',
    '-UsePreparedClientAssets', 'APPIMAGE_EXTRACT_AND_RUN=1',
):
    if marker not in child:
        errors.append(f'Invoke-LinuxRelease.sh missing marker: {marker}')

provision = text('build/wsl/Provision-WslLinuxBuild.sh')
for marker in ('ubuntu|debian','dotnet-sdk-10.0','powershell','python3','rpm','appimagetool-${appimage_arch}.AppImage','~/.local/bin'):
    if marker not in provision:
        errors.append(f'Provision-WslLinuxBuild.sh missing marker: {marker}')
forbid('build/wsl/Provision-WslLinuxBuild.sh', 'docker', 'WSL provisioning must not require Docker.')
forbid('build/wsl/Provision-WslLinuxBuild.sh', 'podman', 'WSL provisioning must not require Podman.')

native = text('build/NativeReleasePackaging.ps1')
for marker in ('$env:ARCH = $appImageArch', "$env:APPIMAGE_EXTRACT_AND_RUN = '1'", '$rpmTarget = "$Architecture-unknown-linux"', '& $rpmbuild --target $rpmTarget', '[switch]$RequireOptionalPackages'):
    if marker not in native:
        errors.append(f'NativeReleasePackaging.ps1 missing Linux packaging marker: {marker}')

# Existing typed policy ownership remains centralized.
store = 'src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs'
req(store, 'RuntimePolicy.MaximumVideoArchiveEntries')
forbid('src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs', 'GetInt("RuntimePolicy.')
forbid('src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs', 'GetInt("RuntimePolicy.')

for rel in ('Components/Pages/Editor.razor','Components/Pages/Help.razor','Components/Pages/Localization.razor','Components/Pages/OrganicPlugins.razor'):
    req('src/PublisherStudio.Web/' + rel, '@rendermode InteractiveServer', f'InteractiveServer boundary missing: {rel}')

if any(x > 9 for x in (2, 1)):
    errors.append('version violates one-digit minor/patch policy')
if errors:
    print('PublisherStudio 3.2.1 static release audit FAILED:')
    for error in errors:
        print(' -', error)
    raise SystemExit(1)
print('PublisherStudio 3.2.1 static release audit passed.')
