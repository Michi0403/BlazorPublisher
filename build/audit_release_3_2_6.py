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

version=(3,2,6)
if any(x>9 for x in version[1:]): errors.append('version violates one-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.2.6</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        data=json.loads(text(rel))
        if data.get('version')!='3.2.6': errors.append(f'{rel} top-level version is not 3.2.6')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
try:
    data=json.loads(text('docs/docfx.json'))
    if data.get('build',{}).get('globalMetadata',{}).get('publisherstudioVersion')!='3.2.6': errors.append('docs/docfx.json publisherstudioVersion is not 3.2.6')
except Exception as exc: errors.append(f'docs/docfx.json JSON parse failed: {exc}')
req('docs/pdf/toc.yml','PublisherStudio-3.2.6.pdf')
req('docs/index.md','**Version 3.2.6**')
req('RELEASE.md','# PublisherStudio 3.2.6')
req('CHANGELOG-v3.2.6-MACOS-ARCHITECTURE-FUTURE2-LICENSING.md','Version advanced from 3.2.5 to 3.2.6.')
req('VALIDATION-v3.2.6-source.md','# PublisherStudio 3.2.6 source validation')

native=text('build/NativeReleasePackaging.ps1')
for marker in (
    'sysctl -n hw.optional.arm64',
    'sysctl -n sysctl.proc_translated',
    'PUBLISHERSTUDIO_NATIVE_REEXEC',
    'exec /usr/bin/arch -arm64 /bin/sh "$0" "$@"',
    'verify_runtime_architecture',
    'native-architecture-manifest.txt',
    'Exact offending file(s):',
    'Remove-NonTargetMacRuntimeAssets $app $Rid',
    'Assert-MacBundleArchitecture $app $Rid',
    '<key>LSArchitecturePriority</key>',
    '<key>LSRequiresNativeExecution</key><true/>',
    'FALLBACK_URL="http://127.0.0.1:58071"',
    'Created and verified headless DMG',
    'Validated PKG payload root /Applications/$appName',
):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing: {marker}')
for bad in ('machine=$(/usr/bin/uname -m','sysctl -in sysctl.proc_translated','tell application "Finder"'):
    if bad in native: errors.append(f'build/NativeReleasePackaging.ps1 retains bad marker: {bad}')

req('README.md','## Future2 role')
req('README.md','centralized corporate or government service')
req('README.md','pwsh ./Build-LocalDevelopment.ps1')
forbid('README.md','Version **3.2.5**')
req('LICENSE.MD','current repository restores .NET packages through NuGet.org')
req('LICENSE.MD','End-user installations do not receive the private developer license.')
forbid('LICENSE.MD','DevExpress NuGet feed')
req('THIRD-PARTY-NOTICES.md','restores .NET packages from NuGet.org')
forbid('THIRD-PARTY-NOTICES.md','configured licensed NuGet/npm feeds')

for rel in ('Components/Pages/Editor.razor','Components/Pages/Help.razor','Components/Pages/Localization.razor','Components/Pages/OrganicPlugins.razor'):
    req('src/PublisherStudio.Web/'+rel,'@rendermode InteractiveServer')
count=0
for p in (ROOT/'src/PublisherStudio.Web').rglob('*.razor'):
    count += p.read_text(encoding='utf-8-sig',errors='replace').count('@rendermode InteractiveServer')
if count != 4: errors.append(f'InteractiveServer occurrence count changed: expected 4, found {count}')

req('build/Ensure-ReleasePackagingPackage.ps1','[string]$Version = "1.0.1"')

if errors:
    print('PublisherStudio 3.2.6 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('PublisherStudio 3.2.6 source audit passed.')
