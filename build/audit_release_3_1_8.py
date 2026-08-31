#!/usr/bin/env python3
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
errors=[]
def text(rel):
    p=ROOT/rel
    if not p.is_file(): errors.append(f'missing file: {rel}'); return ''
    return p.read_text(encoding='utf-8-sig',errors='replace')
def req(rel,needle,msg=None):
    if needle not in text(rel): errors.append(msg or f'{rel} missing: {needle}')
def forbid(rel,needle,msg=None):
    if needle in text(rel): errors.append(msg or f'{rel} contains forbidden: {needle}')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.1.8</Version>')
req('src/PublisherStudio.Web/package.json','"version": "3.1.8"')
req('src/PublisherStudio.Web/package-lock.json','"version": "3.1.8"')
req('docs/docfx.json','"publisherstudioVersion": "3.1.8"')
req('docs/pdf/toc.yml','PublisherStudio-3.1.8.pdf')
req('RELEASE.md','CHANGELOG-v3.1.8-MACOS-NATIVE-BUNDLE-PERMISSIONS.md')
req('RELEASE.md','VALIDATION-v3.1.8-source.md')
text('CHANGELOG-v3.1.8-MACOS-NATIVE-BUNDLE-PERMISSIONS.md'); text('VALIDATION-v3.1.8-source.md')

# Shared packaging remains LocalGPT-owned/local-first.
if (ROOT/'src/LocalGPT.ReleasePackaging').exists(): errors.append('duplicate LocalGPT.ReleasePackaging source present')
if (ROOT/'build/Publish-ReleasePackagingPackage.ps1').exists(): errors.append('duplicate local packaging publisher present')
req('build/Ensure-ReleasePackagingPackage.ps1','[string]$Version = "1.0.1"')
forbid('build/Ensure-ReleasePackagingPackage.ps1','https://github.com/Michi0403/LocalGPT/releases/latest/download/$packageName')

build=text('Build-Release.ps1')
for marker in ('"all-rids"','function Get-ReleaseHostFamily','function Get-HostDefaultRuntimes',"return @('win-x64', 'win-x86', 'win-arm64')", "return @('linux-x64', 'linux-arm64')", "return @('osx-x64', 'osx-arm64')",'[switch]$UseContainerPackaging','Skipping LocalGPT.ReleasePackaging tool preparation because this host-aware release contains Windows runtimes only.'):
    if marker not in build: errors.append(f'Build-Release.ps1 missing host-aware marker: {marker}')

native=text('build/NativeReleasePackaging.ps1')
for marker in ('function Set-UnixExecutable',"& $chmod.Source '0755' $Path",'Set-UnixExecutable (Join-Path $resources $ExecutableName)',"Set-UnixExecutable $Destination","Write-Utf8NoBom (Join-Path $app 'Contents/Info.plist') $infoPlist",'Set-UnixExecutable $appRun','Set-UnixExecutable (Join-Path $appDir $ExecutableName)','Skipping RPM for $Rid','Skipping AppImage for $Rid'):
    if marker not in native: errors.append(f'NativeReleasePackaging.ps1 missing native-mode marker: {marker}')
for forbidden in ('RPM packaging needs rpmbuild, Docker, or Podman.','AppImage needs appimagetool, Docker, or Podman.'):
    if forbidden in native: errors.append(f'optional native packaging still hard-fails: {forbidden}')
mac=native.split("if ($Rid.StartsWith('osx-')) {",1)[1].split("elseif ($Rid.StartsWith('linux-'))",1)[0]
for forbidden in ('New-Rpm','New-AppImage','rpmbuild','appimagetool'):
    if forbidden in mac: errors.append(f'macOS packaging branch unexpectedly contains Linux finisher: {forbidden}')

# Existing central system-variable ownership remains.
store='src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs'
req(store,'RuntimePolicy.MaximumVideoArchiveEntries')
forbid('src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs','GetInt("RuntimePolicy.')
forbid('src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs','GetInt("RuntimePolicy.')

for rel in ('Components/Pages/Editor.razor','Components/Pages/Help.razor','Components/Pages/Localization.razor','Components/Pages/OrganicPlugins.razor'):
    req('src/PublisherStudio.Web/'+rel,'@rendermode InteractiveServer',f'InteractiveServer boundary missing: {rel}')
if any(x>9 for x in (1,8)): errors.append('version violates one-digit minor/patch policy')
if errors:
    print('PublisherStudio 3.1.8 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('PublisherStudio 3.1.8 static release audit passed.')
