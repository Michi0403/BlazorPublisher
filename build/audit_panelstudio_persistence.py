#!/usr/bin/env python3
"""Source-only checks for Panel Studio geometry/template persistence and render boundaries."""
from pathlib import Path
import hashlib
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / 'src/PublisherStudio.Web'
EDITOR = (WEB / 'Components/Pages/Editor.razor').read_text(encoding='utf-8-sig')
PANEL = (WEB / 'Components/Editor/PanelStudio.razor').read_text(encoding='utf-8-sig')
STATE = (WEB / 'Services/EditorStateService.cs').read_text(encoding='utf-8-sig')
JS_PATH = WEB / 'wwwroot/js/publisherInterop.js'
JS = JS_PATH.read_text(encoding='utf-8-sig')
RENDER_AUDIT = (ROOT / 'build/Assert-InteractiveServerRenderModes.ps1').read_text(encoding='utf-8-sig')
MANIFEST = (ROOT / 'build/javascript-diagnostics-files.sha256').read_text(encoding='utf-8-sig')

failures=[]
def req(text, needle, label):
    if needle not in text: failures.append(f'missing {label}: {needle}')

def order(text, first, second, label):
    a=text.find(first); b=text.find(second)
    if a<0 or b<0 or a>b: failures.append(f'wrong/missing ordering for {label}')

for needle,label in [
    ('var fillsLocalCanvas = html is not null','local-canvas equivalence test'),
    ('Math.Abs(html.X) < 0.0001','local X equivalence'),
    ('Math.Abs(html.Y) < 0.0001','local Y equivalence'),
    ('Math.Abs(html.Width - draft.CanvasWidth) < 0.0001','local width equivalence'),
    ('Math.Abs(html.Height - draft.CanvasHeight) < 0.0001','local height equivalence'),
    ('Math.Abs(html.Rotation) < 0.0001','local rotation equivalence'),
    ('State.PromoteSelectedHtmlEmbedToPanel(draft)','promotion path for authored geometry'),
]: req(EDITOR,needle,label)
req(STATE,'replacement.X = selected.X;','outer Mainframe X preservation')
req(STATE,'replacement.Width = selected.Width;','outer Mainframe width preservation')
req(STATE,'_panels.Normalize(Document, replacement);','promoted panel normalization')

req(PANEL,'private async Task SaveSelectedAsTemplate()','async template save')
req(PANEL,'private async Task SaveSelectedAsNewTemplate()','async save-as-new template')
req(PANEL,'private async Task FlushPanelStudioInteractionsAsync()','Blazor queue flush helper')
order(PANEL,'private async Task SaveSelectedAsTemplate()','template.Prototype = Files.CloneElement(SelectedElement);','template save section')
template_block=PANEL[PANEL.find('private async Task SaveSelectedAsTemplate()'):PANEL.find('private async Task SaveSelectedAsNewTemplate()')]
req(template_block,'await FlushPanelStudioInteractionsAsync().ConfigureAwait(false);','flush before template clone')
save_block=PANEL[PANEL.find('private async Task Save()'):PANEL.find('private async Task Save()', PANEL.find('private async Task Save()')+1)] if PANEL.count('private async Task Save()')>1 else PANEL[PANEL.find('private async Task Save()'):]
req(save_block,'await FlushPanelStudioInteractionsAsync().ConfigureAwait(false);','flush before panel apply clone')

req(JS,'export async function flushPanelStudioInteractions(element)','browser queue flush export')
req(JS,'await (binding.invokeQueue || Promise.resolve());','browser queue wait')
req(JS,'flushPanelStudioInteractions(element) { try { return flushPanelStudioInteractions(element);','window.publisherStudio queue flush wrapper')

for rel,directive in [
    ('Components/Pages/Editor.razor','@rendermode InteractiveServer'),
    ('Components/Pages/Help.razor','@rendermode InteractiveServer'),
    ('Components/Pages/Localization.razor','@rendermode InteractiveServer'),
    ('Components/Pages/OrganicPlugins.razor','@rendermode InteractiveServer'),
    ('Components/Layout/JavaScriptDiagnosticsBridge.razor','@rendermode @(new InteractiveServerRenderMode(prerender: false))'),
]:
    text=(WEB/rel).read_text(encoding='utf-8-sig')
    req(text,directive,f'render mode {rel}')
    req(RENDER_AUDIT,rel,f'render-mode audit coverage {rel}')

norm=JS.replace('\r\n','\n').replace('\r','\n').encode('utf-8')
sha=hashlib.sha256(norm).hexdigest()
line=f'{sha}  src/PublisherStudio.Web/wwwroot/js/publisherInterop.js'
req(MANIFEST,line,'current publisherInterop diagnostics hash')

if failures:
    print('Panel Studio persistence source audit failed:')
    for f in failures: print('  -',f)
    raise SystemExit(1)
print('Panel Studio persistence source audit passed: queued interaction commits are flushed before module/panel snapshots, authored single-HTML local geometry promotes to a panel while outer Mainframe bounds remain stable, reviewed InteractiveServer boundaries are covered, and the JS diagnostics hash matches.')
