#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.6.8 fixes."""
from pathlib import Path
import hashlib, json, re, sys

root=Path(__file__).resolve().parents[1]
web=root/'src/PublisherStudio.Web'

def read(rel):
    p=root/rel
    if not p.is_file(): raise AssertionError(f'missing {rel}')
    return p.read_text(encoding='utf-8')

def require(rel,*needles):
    text=read(rel)
    missing=[n for n in needles if n not in text]
    if missing: raise AssertionError(f"{rel} missing {missing}")

try:
    require('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<Version>2.9.0</Version>')
    require('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj','<Version>2.9.0</Version>')

    require('src/PublisherStudio.Web/BusinessObjects/UserNotificationModels.cs','DurationMilliseconds { get; set; } = 10000;')
    require('src/PublisherStudio.Web/Components/Shared/UserNotificationHost.razor',
            'Dictionary<Guid, Timer>', 'SynchronizeExpirationTimers', 'Timeout.InfiniteTimeSpan', 'Notifications.Dismiss(id)')
    require('src/PublisherStudio.Web/Services/UserExperience/UserNotificationService.cs',
            'private readonly object _messagesGate = new();', 'lock (_messagesGate) return _messages.ToArray();')

    require('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor',
            'preview-preset-editing', '_previewPresetEditorVisible ? "preview-preset-editing" : null')
    require('src/PublisherStudio.Web/wwwroot/css/site.css',
            '.panel-studio-canvas-shell.preview-preset-editing{grid-template-rows:auto auto minmax(0,1fr)!important}',
            '.publisher-dialog footer :is(button,.dxbl-btn)',
            '.website-publication .publication-panel-element > :is(.data-visual-view,.devextreme-publication-component,.publication-video-renderer,.live-source-view,.publication-panel)')

    require('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor',
            '"media-studio-sequence-timeline",', './js/mediaStudioInterop.js?v=2.9.0')
    if 'IsVideo ? "media-studio-sequence-timeline" : string.Empty' in read('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor'):
        raise AssertionError('Audio Studio is still excluded from the shared sequence-timeline drag surface')

    require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor',
            'MoveLayerBackward(layer.Id)', 'MoveLayerForward(layer.Id)', 'picture-layer-order')
    require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs',
            'private void MoveLayerForward(Guid layerId)', 'private void MoveLayerBackward(Guid layerId)')

    require('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js',
            'panel.classList.add("ps-pointer-owner")',
            '["datavisual", "devextremecomponent", "livesource"].includes(kind)')
    require('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js',
            "event.target?.closest?.('.ps-pointer-owner,[data-panel-root]')",
            'window.PublisherStudioComponentRuntime?.refreshPanels?.(page);',
            'const qualityCurve = Math.max(.01, Math.min(1, quality)) ** 2;',
            '350_000 + qualityCurve * 2_850_000',
            'audioBitsPerSecond',
            'converted.size < original.size || !options.keepVideoFallback',
            'not "embed both"')

    # Six complete built-in catalogs with key parity. Non-English new catalogs must be meaningfully translated,
    # while technical names, units and protocol literals are allowed to remain canonical.
    loc=web/'Localization'
    cultures=['de-DE','en-US','es-ES','fr-FR','ja-JP','uk-UA']
    catalogs={c:json.loads((loc/f'{c}.json').read_text(encoding='utf-8-sig')) for c in cultures}
    en=catalogs['en-US']
    for c,data in catalogs.items():
        if set(data)!=set(en): raise AssertionError(f'{c} localization key parity failed')
    for c in ['es-ES','fr-FR','ja-JP','uk-UA']:
        changed=sum(1 for k,v in en.items() if catalogs[c][k] != v)
        if changed < 0.70*len(en): raise AssertionError(f'{c} translation coverage too low: {changed}/{len(en)}')

    # Reviewed browser JS manifest stays authoritative after the two runtime edits.
    manifest=read('build/javascript-diagnostics-files.sha256')
    for rel in ['src/PublisherStudio.Web/wwwroot/js/componentRuntime.js','src/PublisherStudio.Web/wwwroot/js/publisherInterop.js']:
        data=(root/rel).read_bytes().replace(b'\r\n',b'\n').replace(b'\r',b'\n')
        line=f'{hashlib.sha256(data).hexdigest()}  {rel}'
        if line not in manifest: raise AssertionError(f'JS diagnostics manifest stale for {rel}')

    # This release must not alter reviewed InteractiveServer boundaries.
    modes=[]
    for path in web.rglob('*.razor'):
        for line in path.read_text(encoding='utf-8').splitlines():
            if '@rendermode' in line: modes.append((str(path.relative_to(root)),line.strip()))
    if len(modes)!=5: raise AssertionError(f'expected 5 PublisherStudio rendermode directives, found {len(modes)}')

    print('PublisherStudio 2.6.8 source regression audit passed.')
except (AssertionError, OSError, json.JSONDecodeError) as exc:
    print(f'PublisherStudio 2.6.8 source regression audit failed: {exc}',file=sys.stderr)
    sys.exit(1)
