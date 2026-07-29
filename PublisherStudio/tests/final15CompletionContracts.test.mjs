import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('Panel Studio renders live content immediately and isolates Mainframe selection while editors are open', () => {
  const panel = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
  const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
  const surface = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
  const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
  assert.match(panel, /DesignerMode="false" DesignPreviewOnly="false"/);
  assert.match(panel, /_previewRevision/);
  assert.match(editor, /SelectionVisualsEnabled="@MainframeSelectionVisualsEnabled"/);
  assert.match(surface, /selection-visuals-suppressed/);
  assert.match(css, /arrange-preview \.publication-panel-element[\s\S]*pointer-events: none/);
  assert.match(css, /arrange-preview \.panel-studio-hitbox[\s\S]*pointer-events: auto/);
});

test('new architecture guards are wired into PublisherStudio direct builds', () => {
  const targets = read('Directory.Build.targets');
  for (const script of ['Assert-MethodDiagnostics.ps1','Assert-ApplicationStaticPolicy.ps1','Assert-TextServiceOwnership.ps1','Assert-IteratorExceptionPolicy.ps1']) {
    assert.match(targets, new RegExp(script.replace('.', '\\.'), 'i'));
    assert.ok(fs.existsSync(path.join(root, 'build', script)));
  }
});
