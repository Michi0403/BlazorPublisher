#!/usr/bin/env python3
from pathlib import Path
import json, sys
ROOT=Path(__file__).resolve().parents[1]
checks=[]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig',errors='replace')
def req(rel,needle,label=None):
    if needle not in read(rel): raise AssertionError(f'{rel}: missing {label or needle!r}')
    checks.append(label or needle)
try:
    for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
        req(rel,'<Version>2.9.2</Version>','2.9.2 package version')
    req('global.json','"version": "10.0.301"','SDK 10.0.301')
    req('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<TargetFramework>net10.0</TargetFramework>','PublisherStudio net10.0')
    req('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<DevExpressVersion>25.2.9</DevExpressVersion>','DevExpress 25.2.9')
    req('Directory.Build.props','<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>','1-Wire 2.1.1')
    page='src/PublisherStudio.Web/Components/Pages/Localization.razor'
    for n in ['IFileLocalizationService LocalizationService','Localization.Editor.PageTitle','Localization.Editor.Title','Localization.Editor.Culture','Localization.Editor.Filter','Localization.Editor.Save','Localization.Editor.SavedTitle','Localization.Editor.SaveFailed','GetCultureDisplayName(culture)','private string L(string key, string fallback)','Logger.LogError']:
        req(page,n,'translation editor:'+n)
    loc=sorted((ROOT/'src/PublisherStudio.Web/Localization').glob('*.json'))
    if len(loc)!=6: raise AssertionError(f'expected 6 localization catalogs, found {len(loc)}')
    sets=[]
    for p in loc: sets.append(set(json.loads(p.read_text(encoding='utf-8-sig'))))
    if any(x!=sets[0] for x in sets[1:]): raise AssertionError('localization key mismatch')
    if len(sets[0])!=3307: raise AssertionError(f'unexpected localization key count {len(sets[0])}')
    for key in ['Localization.Editor.PageTitle','Localization.Editor.Title','Localization.Editor.Save','Localization.Editor.SaveFailed']:
        if key not in sets[0]: raise AssertionError(f'missing {key}')
    checks.append('six localization catalogs / 3307-key parity')
    req('src/PublisherStudio.Web/Components/App.razor','videoEffectRuntime.js?v=2.9.2','2.9.2 runtime cache key')
    req('src/PublisherStudio.Web/Components/App.razor','publisherInterop.js?v=2.9.2','2.9.2 interop cache key')
    req('src/PublisherStudio.Web/Components/Pages/Localization.razor','@rendermode InteractiveServer','translation editor InteractiveServer retained')
    req('RELEASE.md','# PublisherStudio 2.9.2','release file')
    req('CHANGELOG-v2.9.2-TRANSLATION-EDITOR-LOCALIZATION.md','Translation Editor','changelog')
    req('VALIDATION-v2.9.2-source.md','SOURCE-NOT-COMPILED','source-only validation boundary')
    print(f'PublisherStudio 2.9.2 translation-editor source release audit passed: {len(checks)} checks.')
except Exception as exc:
    print(f'PublisherStudio 2.9.2 source release audit failed: {exc}',file=sys.stderr)
    raise SystemExit(1)
