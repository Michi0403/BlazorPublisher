#!/usr/bin/env python3
from pathlib import Path
import json,re
ROOT=Path(__file__).resolve().parents[1]
FAIL=[]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def req(ok,msg):
    if not ok: FAIL.append(msg)
for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
    req('<Version>3.0.8</Version>' in read(rel), f'{rel} is not 3.0.8')
for rel in ['src/PublisherStudio.Web/Components/App.razor','src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Editor/MediaStudio.razor','src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor','docs/docfx.json','docs/pdf-cover.html','docs/pdf/toc.yml','docs/index.md','RELEASE.md']:
    req('3.0.8' in read(rel), f'{rel} current identity is not 3.0.8')
for rel in ['src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json']:
    req('3.0.8' in read(rel), f'{rel} package identity is not 3.0.8')
req((ROOT/'CHANGELOG-v3.0.8-MACOS-DOCUMENTATION-PDF-RUNTIME-REPAIR.md').is_file(), '3.0.8 changelog missing')
req((ROOT/'VALIDATION-v3.0.8-source.md').is_file(), '3.0.8 validation missing')
doc=read('build/Build-Documentation.ps1')
for path in [
    'Google Chrome.app/Contents/MacOS/Google Chrome',
    'Microsoft Edge.app/Contents/MacOS/Microsoft Edge',
    'Chromium.app/Contents/MacOS/Chromium']:
    req(path in doc, f'macOS browser probe missing: {path}')
req("[string]::Equals($unixName.Trim(), 'Darwin', [StringComparison]::OrdinalIgnoreCase)" in doc, 'Darwin guard missing')
req('$capturedOutput = [System.Collections.Generic.List[string]]::new()' in doc, 'DocFX streaming capture missing')
req('& dotnet tool run docfx @Arguments 2>&1 | ForEach-Object' in doc, 'manifest DocFX output is not streamed')
req('& $script:docfxExecutable @Arguments 2>&1 | ForEach-Object' in doc, 'fallback DocFX output is not streamed')
req("$rawLine -split \"`r\"" in doc, 'carriage-return output normalization missing')
req("(?:Removed|Copied)\\s+\\d+\\s+of\\s+\\d+\\s+files" in doc, 'corrupt transfer-counter display filter missing')
req('@("pdf", $configPath, "--logLevel", "verbose")' in doc, 'PDF fallback does not expose verbose live diagnostics')
req('Assert-PublisherStudioGeneratedHtmlPreflight -SiteRoot $siteRoot' in doc, 'strict HTML accessibility/link preflight missing')
req('Repair-PublisherStudioGeneratedHtmlLanguage -SiteRoot $siteRoot' in doc, '3.0.7 language normalization missing')
for v in re.findall(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', '\n'.join(read(x) for x in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'])):
    req(len(v[1])==1 and len(v[2])==1, f'two-digit minor/patch slot: {v}')
try:
    pkg=json.loads(read('src/PublisherStudio.Web/package.json')); lock=json.loads(read('src/PublisherStudio.Web/package-lock.json'))
    req(pkg.get('version')=='3.0.8','package.json version mismatch')
    req(lock.get('version')=='3.0.8','package-lock.json top version mismatch')
except Exception as exc: FAIL.append(f'package JSON parse failed: {exc}')
if FAIL: raise SystemExit('PublisherStudio 3.0.8 audit failed:\n - '+'\n - '.join(FAIL))
print('PublisherStudio 3.0.8 static release audit passed.')
