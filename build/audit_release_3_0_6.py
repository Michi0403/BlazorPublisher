#!/usr/bin/env python3
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
FAIL=[]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def req(ok,msg):
    if not ok: FAIL.append(msg)
for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj','src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json']:
    req('3.0.6' in read(rel), f'{rel} is not 3.0.6')
for rel in ['src/PublisherStudio.Web/Components/App.razor','src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Editor/MediaStudio.razor','src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor','docs/docfx.json','docs/pdf-cover.html','docs/pdf/toc.yml','docs/index.md','RELEASE.md']:
    req('3.0.6' in read(rel), f'{rel} current identity is not 3.0.6')
pkg=json.loads(read('src/PublisherStudio.Web/package.json'))
parts=pkg['version'].split('.')
req(pkg['version']=='3.0.6' and len(parts[1])==1 and len(parts[2])==1, 'single-digit version policy failed')
doc=read('build/Build-Documentation.ps1')
lang_marker="if ($updated -notmatch '(?i)<html\\b[^>]*\\blang\\s*=\\s*[\"''][^\"'']+[\"'']')"
req(lang_marker in doc, 'generated HTML lang normalization guard missing')
req("'<html lang=\"en\"'" in doc, 'generated HTML lang insertion missing')
lang_pos=doc.find('DocFX modern API pages can omit the html language attribute')
theme_inject_pos=doc.find("if ($updated -notmatch '(?i)data-publisherstudio-theme-bootstrap')", lang_pos)
req(lang_pos >= 0 and theme_inject_pos > lang_pos, 'lang normalization is not inside the HTML theme pass before theme-bootstrap injection')
req(doc.find('Install-PublisherStudioWebsiteThemeAssets -SiteRoot $siteRoot') < doc.find('Assert-PublisherStudioGeneratedHtmlPreflight -SiteRoot $siteRoot'), 'theme normalization does not execute before accessibility preflight')
req((ROOT/'CHANGELOG-v3.0.6-DOCFX-HTML-LANGUAGE-NORMALIZATION.md').is_file(), '3.0.6 changelog missing')
req((ROOT/'VALIDATION-v3.0.6-source.md').is_file(), '3.0.6 validation missing')
native=read('src/PublisherStudio.Web/Services/Streaming/Capture/NativeDeviceDiscoveryPlatformServices.cs')
req('global::PublisherStudio.BusinessObjects.PublisherRuntimePattern.NativeDirectShowDevice' in native, 'DirectShow qualified runtime pattern lost')
req('global::PublisherStudio.BusinessObjects.PublisherRuntimePattern.NativeAvFoundationDevice' in native, 'AVFoundation qualified runtime pattern lost')
if FAIL: raise SystemExit('PublisherStudio 3.0.6 audit failed:\n - '+'\n - '.join(FAIL))
print('PublisherStudio 3.0.6 static release audit passed.')
