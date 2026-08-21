#!/usr/bin/env python3
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
fail=[]
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def req(rel,*needles):
    s=text(rel)
    for n in needles:
        if n not in s: fail.append(f'{rel}: missing {n}')
def forbid(rel,*needles):
    s=text(rel)
    for n in needles:
        if n in s: fail.append(f'{rel}: forbidden stale token {n}')
for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
    req(rel,'<Version>2.9.3</Version>')
req('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<TargetFramework>net10.0</TargetFramework>','<DevExpressVersion>25.2.9</DevExpressVersion>')
req('src/PublisherStudio.Web/Components/App.razor','dx.all.js?v=25.2.9','dx.light.css?v=25.2.9','devextreme-license.js?v=25.2.9','componentRuntime.js?v=2.9.3','publisherInterop.js?v=2.9.3','site.css?v=2.9.3')
req('src/PublisherStudio.Web/BusinessObjects/PublicationModels.cs','List<PublicationBehavior> Behaviors')
req('src/PublisherStudio.Web/BusinessObjects/PublicationBehaviorModels.cs','PublicationBehaviorTrigger','PublicationBehaviorAction','PublicationObjectAddressOption')
req('src/PublisherStudio.Web/Services/PublicationBehaviorService.cs','publication://','CommonMethods','ScriptHelpers','HtmlScriptHelpers','ScriptCall','Serialize')
req('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor','Behavior &amp; object interface','Object address','Add behavior','Add click script','DxContextMenu','QuickBehaviorRefresh','SelectBehaviorEditor')
req('src/PublisherStudio.Web/Components/Editor/PanelView.razor','data-object-address','data-behaviors')
req('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js','PublisherStudioPublicationRuntime','resolvePublicationObject','invokeObjectMethod','data-behaviors','publicationObjectsApi')
req('src/PublisherStudio.Web/tools/prepare-devexpress-assets.mjs','removeGeneratedDirectory','restoredPackageVersion','copiedDevExtremeVersion','devextreme-assets.meta.json')
req('Prepare-DevExpressAssets.ps1','Remove-GeneratedPathWithRetry','Clearing generated DevExpress browser asset folders','devextreme-dist','copiedPackageJson.version')
req('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js','devextreme-assets.meta.json','devextreme-dist/package.json',"cache: 'no-store'",'preparedDevExtremeVersion','copiedDevExtremeVersion')
forbid('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js','bundled browser runtime is')
req('THIRD-PARTY-NOTICES.md','DevExpress Blazor 25.2.9','DevExtreme 25.2.9')
req('CHANGELOG-v2.9.3-PANEL-BEHAVIORS-DEVEXTREME-ASSET-REPAIR.md','PublisherStudio 2.9.3')
req('VALIDATION-v2.9.3-source.md','source-only and not compiled')
if fail:
    print('PublisherStudio 2.9.3 release audit failed:')
    print('\n'.join(' - '+x for x in fail)); sys.exit(1)
print('PublisherStudio 2.9.3 release audit passed: Panel Studio behavior/object interface, helper UX, 25.2.9 cache/version/package validation, Windows vendor cleanup, .NET 10 and release alignment are present.')
