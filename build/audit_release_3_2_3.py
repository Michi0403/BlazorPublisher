#!/usr/bin/env python3
from pathlib import Path
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

def text(rel):
    p = ROOT / rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig', errors='replace')

def req(rel, needle):
    if needle not in text(rel):
        errors.append(f'{rel} missing: {needle}')

def forbid(rel, needle):
    if needle in text(rel):
        errors.append(f'{rel} contains forbidden: {needle}')

version = (3, 2, 3)
if any(x > 9 for x in version[1:]):
    errors.append('version violates one-digit minor/patch policy')

for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    req(rel, '<Version>3.2.3</Version>')
    try:
        ET.parse(ROOT / rel)
    except Exception as exc:
        errors.append(f'{rel} XML parse failed: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        data = json.loads(text(rel))
        if data.get('version') != '3.2.3':
            errors.append(f'{rel} top-level version is not 3.2.3')
    except Exception as exc:
        errors.append(f'{rel} JSON parse failed: {exc}')

req('docs/docfx.json', '"publisherstudioVersion": "3.2.3"')
req('docs/pdf/toc.yml', 'PublisherStudio-3.2.3.pdf')
req('RELEASE.md', 'CHANGELOG-v3.2.3-MACOS-COORDINATOR-PAGES-PACKAGING.md')
req('RELEASE.md', 'VALIDATION-v3.2.3-source.md')
text('CHANGELOG-v3.2.3-MACOS-COORDINATOR-PAGES-PACKAGING.md')
text('VALIDATION-v3.2.3-source.md')

release = text('Build-Release.ps1')
for marker in (
    '$ProgressPreference = "SilentlyContinue"',
    '$buildStateDirectories = @(',
    'Remove-Item -LiteralPath $buildStateDirectory.FullName -Recurse -Force -ErrorAction Stop',
    'Durable documentation caches outside bin/obj were preserved.',
    '& (Join-Path $root \'build/Assert-InteractiveServerRenderModes.ps1\')',
):
    if marker not in release:
        errors.append(f'Build-Release.ps1 missing deterministic/preserved marker: {marker}')

doc = text('build/Build-Documentation.ps1')
for marker in (
    '$ProgressPreference = "SilentlyContinue"',
    'payload-cache/PublisherStudio',
    'Get-PublisherStudioDocumentationCacheKey',
    'Save-PublisherStudioDocumentationHtmlCache',
    'Save-PublisherStudioDocumentationPdfCache',
    'Reused durable PublisherStudio DocFX HTML cache',
    'Skipping DocFX tool restore because validated PublisherStudio HTML was restored from the durable documentation cache.',
    '$pdfTimeoutMilliseconds = 1800000',
    '$configuredPdfTimeout -gt 0',
    'localgpt-publisherstudio-docfx-pdf.lock',
    'Enter-PublisherStudioSharedPdfLock',
    'cached-validated-pdf',
):
    if marker not in doc:
        errors.append(f'build/Build-Documentation.ps1 missing resilience marker: {marker}')
for forbidden in (
    '$pdfTimeoutMilliseconds = if ($isMacOsHost) { 300000 } else { 1800000 }',
    'elseif ($docfxBuildSucceeded) {\n        $warnings.Add("Complete PDF generation was explicitly disabled',
):
    if forbidden in doc:
        errors.append(f'build/Build-Documentation.ps1 retains broken marker: {forbidden}')
if doc.find('Save-PublisherStudioDocumentationHtmlCache') > doc.find('Enter-PublisherStudioSharedPdfLock'):
    errors.append('durable HTML cache is not committed before the long PDF-render lock/stage')

native = text('build/NativeReleasePackaging.ps1')
for marker in (
    "$ProgressPreference = 'SilentlyContinue'",
    'function New-Dmg',
    'hdiutil create -volname $volumeName -srcfolder $stage -ov -format UDZO',
    'hdiutil verify $Destination',
    "New-Item -ItemType SymbolicLink -Path (Join-Path $stage 'Applications') -Target '/Applications'",
    'function New-MacPkg',
    '--root $pkgRoot',
    "--install-location '/'",
    'pkgutil --payload-files',
    '$stagedInfoPlist = Join-Path $applicationsRoot "$appName/Contents/Info.plist"',
):
    if marker not in native:
        errors.append(f'build/NativeReleasePackaging.ps1 missing packaging marker: {marker}')
for forbidden in (
    'tell application "Finder"',
    'set background picture of theViewOptions',
    'hdiutil attach $rwDmg',
    'hdiutil convert $rwDmg',
    '--component $AppPath',
):
    if forbidden in native:
        errors.append(f'build/NativeReleasePackaging.ps1 retains unreliable packaging path: {forbidden}')

# Preserve the existing server-interactive boundaries explicitly guarded by the project.
for rel in ('Components/Pages/Editor.razor','Components/Pages/Help.razor','Components/Pages/Localization.razor','Components/Pages/OrganicPlugins.razor'):
    req('src/PublisherStudio.Web/' + rel, '@rendermode InteractiveServer')

# PublisherStudio still consumes the LocalGPT-owned 1.0.1 packaging helper and keeps Pages HTML-only.
req('build/Ensure-ReleasePackagingPackage.ps1', '[string]$Version = "1.0.1"')
req('.github/scripts/prepare-pages-artifact.py', 'pagesPdfPublished')
try:
    json.loads(text('docs/docfx.json'))
except Exception as exc:
    errors.append(f'docs/docfx.json JSON parse failed: {exc}')

if errors:
    print('PublisherStudio 3.2.3 static release audit FAILED:')
    for error in errors:
        print(' -', error)
    raise SystemExit(1)
print('PublisherStudio 3.2.3 static release audit passed.')
