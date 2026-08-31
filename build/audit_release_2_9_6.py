#!/usr/bin/env python3
"""Source-only release audit for PublisherStudio 2.9.6."""
from pathlib import Path
import hashlib, json, re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / 'src/PublisherStudio.Web'
failures: list[str] = []
checks: list[str] = []

def req(text: str, needle: str, label: str) -> None:
    if needle not in text:
        failures.append(f'missing {label}: {needle}')
    else:
        checks.append(label)

def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding='utf-8-sig')

# Version contract and no two-digit minor/patch slots.
for rel in [
    'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
    'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj',
]:
    text = read(rel)
    req(text, '<Version>2.9.6</Version>', f'2.9.6 version in {rel}')
package = json.loads(read('src/PublisherStudio.Web/package.json'))
lock = json.loads(read('src/PublisherStudio.Web/package-lock.json'))
if package.get('version') != '2.9.6' or lock.get('version') != '2.9.6' or lock.get('packages',{}).get('',{}).get('version') != '2.9.6':
    failures.append('npm package/package-lock version alignment is not 2.9.6')
else: checks.append('npm package/package-lock 2.9.6 alignment')
major, minor, patch = map(int, package['version'].split('.'))
if minor >= 10 or patch >= 10: failures.append('release version violates single-digit minor/patch policy')
else: checks.append('single-digit minor/patch release policy')

# Template libraries and non-destructive seed wiring.
service = read('src/PublisherStudio.Web/Services/Publication/PublisherTemplateLibraryService.cs')
for needle, label in [
    ('"PublisherTemplates"', 'PublisherTemplates LocalApplicationData directory'),
    ('"DivTemplates"', 'DivTemplates LocalApplicationData directory'),
    ('File.Exists(targetPath)) continue;', 'non-destructive seed copy'),
    ('SearchOption.TopDirectoryOnly', 'top-level template discovery boundary'),
    ('Path.GetFileName(templateId)', 'template path traversal boundary'),
    ('RegenerateDetachedPanelIdentity(document, panel);', 'Div insertion identity regeneration'),
    ('behavior.TargetElementId = MapLocalElementReference', 'Div behavior reference remap'),
    ('RemapEndpoint(connector.Source', 'Div connector endpoint remap'),
]: req(service, needle, label)
req(read('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs'), 'AddSingleton<IPublisherTemplateLibraryService, PublisherTemplateLibraryService>', 'template library DI registration')
req(read('src/PublisherStudio.Web/Program.cs'), 'EnsureTemplateDirectories()', 'startup template directory seeding')

publisher_dir = WEB / 'Configuration/Templates/Publisher'
div_dir = WEB / 'Configuration/Templates/Div'
for name, minimum_pages in [('photo-blog.pubstudio.json', 4), ('business-presentation.pubstudio.json', 5)]:
    path = publisher_dir / name
    try: doc = json.loads(path.read_text(encoding='utf-8-sig'))
    except Exception as exc:
        failures.append(f'{name} invalid JSON: {exc}'); continue
    pages = doc.get('pages') or []
    if len(pages) < minimum_pages: failures.append(f'{name} has only {len(pages)} page(s)')
    else: checks.append(f'{name} multi-page starter')
    if not all((page.get('transition') or {}).get('kind') for page in pages): failures.append(f'{name} lacks maintained page transitions')
    else: checks.append(f'{name} page transitions')
    animation_count = 0
    def walk(value):
        nonlocal_box[0] += len(value.get('animations') or []) if isinstance(value, dict) else 0
        if isinstance(value, dict):
            for child in value.values(): walk(child)
        elif isinstance(value, list):
            for child in value: walk(child)
    nonlocal_box=[0]; walk(pages); animation_count=nonlocal_box[0]
    if animation_count <= 0: failures.append(f'{name} contains no authored object animations')
    else: checks.append(f'{name} authored animations')
for name in ['media-hero.divtemplate.json','two-view-info.divtemplate.json','kpi-strip.divtemplate.json']:
    try: doc=json.loads((div_dir/name).read_text(encoding='utf-8-sig'))
    except Exception as exc:
        failures.append(f'{name} invalid JSON: {exc}'); continue
    proto=doc.get('prototype',doc)
    if str(proto.get('$type','')).lower() != 'panel' or not proto.get('views'):
        failures.append(f'{name} is not a reusable PanelElement template')
    else: checks.append(f'{name} PanelElement starter')

# New / Panel Library / Panel Studio integration.
new_dialog = read('src/PublisherStudio.Web/Components/Editor/NewPublicationDialog.razor')
panel_library = read('src/PublisherStudio.Web/Components/Editor/PanelLibrary.razor')
panel_studio = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor')
ribbon = read('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor')
editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor')
for text, needle, label in [
    (ribbon, 'New from template', 'File/New-from-template command'),
    (new_dialog, 'PublisherTemplateDirectory', 'publication-template chooser path'),
    (panel_library, 'DivTemplateDirectory', 'Panel Library local Div section'),
    (panel_studio, 'divtemplate:', 'Panel Studio Div-template palette tools'),
    (editor, 'CreatePublicationFromTemplate', 'editor publication-template creation'),
    (editor, 'InsertDivTemplate', 'editor Div-template insertion'),
]: req(text, needle, label)

# Signal/component/media contract.
models = read('src/PublisherStudio.Web/BusinessObjects/PublicationModels.cs')
for trigger in ['OnClick','OnDoubleClick','OnChange','OnFocus','OnBlur','OnPlay','OnPause','OnEnded','OnItemClick','OnSelectionChanged','OnValueChanged','OnSubmit','OnRowInserted','OnRowUpdated','OnRowRemoved','OnAppointmentAdded','OnAppointmentUpdated','OnAppointmentDeleted','OnMessageEntered']:
    req(models, trigger, f'signal trigger {trigger}')
req(models, 'CallMethod', 'signal CallMethod completion action')
req(models, 'CompletionMethod', 'signal completion method storage')
inspector = read('src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor')
req(inspector, 'FlattenSignalTarget', 'nested Panel/Div signal target discovery')
req(inspector, 'Behaviors.CommonMethods(target)', 'component-specific signal method allow-list')
behavior = read('src/PublisherStudio.Web/Services/PublicationBehaviorService.cs')
for method in ['"click"','"focus"','"blur"','"change"','"show"','"hide"','"enable"','"disable"','"setValue"','"refresh"','"clearFilter"','"clearSelection"','"selectAll"','"play"','"pause"','"togglePlayback"','"mute"','"unmute"','"setVolume"','"seek"']:
    req(behavior, method, f'common method {method}')

component_js = read('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js')
signal_js = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js')
for text, needle, label in [
    (component_js, 'publicationNativeControlOwnsEvent', 'native media/control click ownership guard'),
    (component_js, 'publisherstudio:component-event', 'maintained component event bridge'),
    (component_js, 'async function invokeObjectMethod', 'publication object method runtime'),
    (signal_js, "host.addEventListener('play'", 'signal OnPlay listener'),
    (signal_js, "host.addEventListener('pause'", 'signal OnPause listener'),
    (signal_js, "host.addEventListener('ended'", 'signal OnEnded listener'),
    (signal_js, "host.addEventListener('publisherstudio:component-event'", 'signal component-event listener'),
    (signal_js, 'signalMediaSuppression = new WeakMap()', 'signal media recursion suppression'),
    (signal_js, 'nativeControlOwnsEvent(event)', 'signal native-control click guard'),
    (signal_js, "['play','pause','toggleplayback','mute','unmute','setvolume','seek']", 'signal CallMethod media suppression coverage'),
]: req(text, needle, label)
for method in ['change','toggleplayback','mute','unmute','setvolume','seek']:
    req(component_js.lower(), f'name === "{method}"', f'object runtime method {method}')

# Nested PanelView objects retain canonical element IDs used by signal lookup.
req(read('src/PublisherStudio.Web/Components/Editor/PanelView.razor'), 'data-element-id="@element.Id"', 'Panel/Div child DOM element identity')

# Localization parity and new phrases.
locales = ['en-US','de-DE','es-ES','fr-FR','ja-JP','uk-UA']
catalogs = {culture: json.loads((WEB/f'Localization/{culture}.json').read_text(encoding='utf-8-sig')) for culture in locales}
keysets = [set(catalogs[c]) for c in locales]
if not all(keys == keysets[0] for keys in keysets[1:]): failures.append('PublisherStudio localization catalogs are not in exact key parity')
else: checks.append(f'six localization catalogs / {len(keysets[0])}-key parity')
for phrase in ['New from template','Local Div templates','Method','Created from local publisher template','Inserted from local Div template']:
    if phrase not in set(catalogs['en-US'].values()): failures.append(f'missing localized English phrase: {phrase}')
    else: checks.append(f'localized phrase {phrase}')

# Maintained InteractiveServer boundary remains on routed editor; nested components inherit it.
req(editor, '@rendermode InteractiveServer', 'Editor InteractiveServer render boundary')

# Diagnostics manifest matches changed JS exactly.
manifest = read('build/javascript-diagnostics-files.sha256')
for rel in ['src/PublisherStudio.Web/wwwroot/js/componentRuntime.js','src/PublisherStudio.Web/wwwroot/js/publisherInterop.js']:
    data=read(rel).replace('\r\n','\n').replace('\r','\n').encode('utf-8')
    line=f'{hashlib.sha256(data).hexdigest()}  {rel}'
    if line not in manifest: failures.append(f'JavaScript diagnostics hash mismatch for {rel}')
    else: checks.append(f'JavaScript diagnostics hash {Path(rel).name}')

if failures:
    print('PublisherStudio 2.9.6 source release audit failed:')
    for failure in failures: print('  -', failure)
    raise SystemExit(1)
print(f'PublisherStudio 2.9.6 source release audit passed: {len(checks)} checks.')
