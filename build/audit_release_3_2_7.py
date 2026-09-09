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

version=(3,2,7)
if any(x>9 for x in version[1:]): errors.append('version violates one-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel,'<Version>3.2.7</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        data=json.loads(text(rel))
        if data.get('version')!='3.2.7': errors.append(f'{rel} top-level version is not 3.2.7')
        if rel.endswith('package-lock.json') and data.get('packages',{}).get('',{}).get('version')!='3.2.7':
            errors.append('package-lock.json root package version is not 3.2.7')
    except Exception as exc: errors.append(f'{rel} JSON parse failed: {exc}')
try:
    data=json.loads(text('docs/docfx.json'))
    if data.get('build',{}).get('globalMetadata',{}).get('publisherstudioVersion')!='3.2.7': errors.append('docs/docfx.json publisherstudioVersion is not 3.2.7')
except Exception as exc: errors.append(f'docs/docfx.json JSON parse failed: {exc}')
req('docs/pdf/toc.yml','PublisherStudio-3.2.7.pdf')
req('docs/index.md','**Version 3.2.7**')
req('RELEASE.md','# PublisherStudio 3.2.7')
req('CHANGELOG-v3.2.7-METHOD-DIAGNOSTICS-BUILD-REPAIR.md','Version advanced from 3.2.6 to 3.2.7')
req('VALIDATION-v3.2.7-source.md','# PublisherStudio 3.2.7 source validation')

service='src/PublisherStudio.Web/Services/Configuration/ApplicationPathService.cs'
req(service,'private string KnownFolderOrFallback(Environment.SpecialFolder folder, string fallback)')
req(service,'logger.LogTrace($"Entering ApplicationPathService.KnownFolderOrFallback for {folder}.")')
req(service,'logger.LogError(exception, $"ApplicationPathService.KnownFolderOrFallback failed for {folder}: {exception.Message}")')
forbid(service,'private static string KnownFolderOrFallback')

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

req('README.md','## Future2 role')
req('LICENSE.MD','current repository restores .NET packages through NuGet.org')
req('THIRD-PARTY-NOTICES.md','restores .NET packages from NuGet.org')

for rel in ('Components/Pages/Editor.razor','Components/Pages/Help.razor','Components/Pages/Localization.razor','Components/Pages/OrganicPlugins.razor'):
    req('src/PublisherStudio.Web/'+rel,'@rendermode InteractiveServer')
count=0
for p in (ROOT/'src/PublisherStudio.Web').rglob('*.razor'):
    count += p.read_text(encoding='utf-8-sig',errors='replace').count('@rendermode InteractiveServer')
if count != 4: errors.append(f'InteractiveServer occurrence count changed: expected 4, found {count}')

req('build/Ensure-ReleasePackagingPackage.ps1','[string]$Version = "1.0.1"')

if errors:
    print('PublisherStudio 3.2.7 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('PublisherStudio 3.2.7 source audit passed.')
