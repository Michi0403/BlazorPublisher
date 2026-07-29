import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('Panel Studio keeps one browser binding across render and mode changes', () => {
  const panel = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
  const js = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
  assert.match(panel, /private readonly string _interactionBindingId = Guid\.NewGuid\(\)\.ToString\("N"\)/);
  assert.match(panel, /data-panel-studio-binding-id="@_interactionBindingId"/);
  assert.match(panel, /var bindingKey = _draft is null \? null : _interactionBindingId/);
  assert.match(panel, /bindPanelStudioDropSurface", _canvasElement, _self, _interactionBindingId/);
  const modeBlock = panel.slice(panel.indexOf('private void EnableInteractionPreview()'), panel.indexOf('private Task EditSelectedComponent'));
  assert.doesNotMatch(modeBlock, /_dropSurfaceBound = false|_dropSurfaceBindingKey = null/);
  assert.match(js, /existing\.bindingId === normalizedBindingId/);
  assert.match(js, /existing\.dotNetReference = dotNetReference \|\| existing\.dotNetReference/);
});

test('Panel Studio does not switch to interact through background input and logs cancellation context', () => {
  const panel = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
  const js = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
  assert.doesNotMatch(js, /panelStudioInvoke\(binding, 'interact'\)/);
  assert.doesNotMatch(panel, /case "interact":/);
  assert.match(js, /operation=\$\{operation\}; binding=\$\{binding\?\.bindingId/);
  assert.match(panel, /TokenCancellationRequested:\{exception\.CancellationToken\.IsCancellationRequested\}/);
  assert.match(panel, /Panel Studio browser interaction ended normally\. Binding:/);
});

test('Panel Studio lifecycle validation remains available explicitly', () => {
  const guard = read('build/Assert-PanelStudioInteractionLifecycle.ps1');
  assert.match(guard, /Repeated renders must reuse the existing binding/);
  assert.match(guard, /Gamepad or keyboard interop must not switch/);
});
