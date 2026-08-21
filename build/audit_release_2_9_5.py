#!/usr/bin/env python3
from pathlib import Path
import sys
ROOT = Path(__file__).resolve().parents[1]
fail = []
def text(rel): return (ROOT / rel).read_text(encoding='utf-8-sig')
def req(rel, *needles):
    source = text(rel)
    for needle in needles:
        if needle not in source: fail.append(f'{rel}: missing {needle}')
def forbid(rel, *needles):
    source = text(rel)
    for needle in needles:
        if needle in source: fail.append(f'{rel}: forbidden stale/invalid token {needle}')
for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj', 'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
    req(rel, '<Version>2.9.5</Version>')
req('src/PublisherStudio.Web/package.json', '"version": "2.9.5"', '"devextreme-dist": "25.2.9"')
req('src/PublisherStudio.Web/package-lock.json', '"version": "2.9.5"', 'devextreme-dist-25.2.9.tgz')
req('src/PublisherStudio.Web/PublisherStudio.Web.csproj', '<TargetFramework>net10.0</TargetFramework>', '<DevExpressVersion>25.2.9</DevExpressVersion>', 'DevExtremeRuntimeKeyGeneratorVersionFile')
req('src/PublisherStudio.Web/Components/App.razor', 'dx.all.js?v=25.2.9', 'devextreme-license.js?v=25.2.9', 'componentRuntime.js?v=2.9.5', 'publisherInterop.js?v=2.9.5', 'site.css?v=2.9.5')
app = text('src/PublisherStudio.Web/Components/App.razor')
if not (app.index('dx.all.js?v=25.2.9') < app.index('devextreme-license.js?v=25.2.9') < app.index('<Routes />')):
    fail.append('App.razor: generated non-modular runtime key must load immediately after dx.all.js and before Routes')
req('src/PublisherStudio.Web/tools/resolve-devextreme-package-root.mjs', 'devextreme-license.js', 'expectedVersion')
req('src/PublisherStudio.Web/tools/prepare-devexpress-assets.mjs', 'PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT', 'overlayAuthoritativeDevExtremeRuntime', 'authoritativeRuntimePackageVersion', 'schemaVersion: 4', 'console.warn')
forbid('src/PublisherStudio.Web/tools/prepare-devexpress-assets.mjs', 'Restored devextreme-dist is ${restoredDevExtremeVersion')
req('Prepare-DevExpressAssets.ps1', 'resolve-devextreme-package-root.mjs', 'bin\\devextreme-license.js', 'runtimeLicenseGeneratedPath', 'PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT', 'generatorPackageVersion', 'Get-FileHash', 'Remove-GeneratedPathWithRetry')
req('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js', 'devextreme-license.meta.json', 'runtimeKeyGeneratorVersion', 'authoritativeRuntimeVersion', "cache: 'no-store'", 'actualLicenseHash', 'globalThis.DevExpress?.VERSION')
forbid('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js', 'license targets', 'bundled browser runtime is')
req('src/PublisherStudio.Web/BusinessObjects/PublicationModels.cs', 'List<PublicationBehavior> Behaviors')
req('src/PublisherStudio.Web/Services/PublicationBehaviorService.cs', 'publication://', 'CommonMethods', 'ScriptHelpers')
panel = text('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor')
for needle in ['@(method + "()")', '<option value="@(page.Id)">@(page.Name)</option>', 'title="@(helper.Description)"', '>@(helper.Label)</button>']:
    if needle not in panel: fail.append(f'PanelStudio.razor: missing Razor parser compatibility expression {needle}')
for needle in ['@method()</option>', '<option value="@page.Id">@page.Name</option>', 'title="@helper.Description"', '>@helper.Label</button>']:
    if needle in panel: fail.append(f'PanelStudio.razor: stale Razor parser collision remains: {needle}')
req('CHANGELOG-v2.9.5-PANEL-STUDIO-RAZOR-PARSER-COMPATIBILITY.md', 'PublisherStudio 2.9.5', 'RZ9979', 'CS0149')
req('VALIDATION-v2.9.5-source.md', 'source-only and not compiled')
req('RELEASE.md', 'PublisherStudio 2.9.5', 'SOURCE-NOT-COMPILED')
if fail:
    print('PublisherStudio 2.9.5 release audit failed:')
    print('\n'.join(' - ' + item for item in fail))
    sys.exit(1)
print('PublisherStudio 2.9.5 release audit passed: Razor parser compatibility, retained Panel Studio behaviors, DevExtreme 25.2.9 provenance repair, .NET 10 and release alignment are present.')
