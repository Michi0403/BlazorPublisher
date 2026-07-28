import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('Panel Studio window interop preserves the DotNet reference and exposes pointer cancellation', () => {
  const js = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
  assert.match(js, /bindPanelStudioDropSurface\(element, dotNetReference, bindingId = ''\)\s*\{\s*try\s*\{\s*return bindPanelStudioDropSurface\(element, dotNetReference, bindingId\);/);
  assert.match(js, /cancelPanelStudioPointer\(element, restore = true\)\s*\{\s*try\s*\{\s*cancelPanelStudioPointer\(element, restore\);/);
  assert.match(js, /if \(!binding\.dotNetReference\) throw new Error\('Panel Studio \.NET interaction reference is unavailable\.'/);
  assert.match(js, /reportingError: false/);
});

test('Panel Studio binding and JS callbacks are guarded and renderer-affine', () => {
  const panel = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
  assert.match(panel, /InvokeAsync<bool>\("publisherStudio\.bindPanelStudioDropSurface", _canvasElement, _self, _interactionBindingId\)\.ConfigureAwait\(true\)/);
  assert.match(panel, /RunPanelInteropAsync\("CommitBounds"/);
  assert.match(panel, /RunPanelInteropAsync\("SelectElement"/);
  assert.match(panel, /RunPanelInteropAsync\("ActivateElement"/);
  assert.match(panel, /RunPanelInteropAsync\("Command"/);
  assert.match(panel, /catch \(JSException exception\)/);
  assert.doesNotMatch(panel, /ConfigureAwait\(false\)/);
  const hitLayerStart = panel.indexOf('<div class="panel-studio-hit-layer"');
  const hitLayerEnd = panel.indexOf('@if (_interactionPreview)', hitLayerStart);
  const hitLayer = panel.slice(hitLayerStart, hitLayerEnd);
  assert.ok(hitLayerStart >= 0 && hitLayerEnd > hitLayerStart);
  assert.doesNotMatch(hitLayer, /@onclick="\(\) => SelectElement\(element\.Id\)"/);
  assert.doesNotMatch(hitLayer, /@ondblclick="\(\) => ActivateElementInteraction\(element\.Id\)"/);
});

test('PublisherStudio direct builds enforce render, async, and component diagnostic policies', () => {
  const targets = read('Directory.Build.targets');
  for (const name of [
    'AssertPublisherInteractiveServerRenderModes',
    'AssertPublisherAsyncContinuationPolicy',
    'AssertPublisherComponentDiagnostics'
  ]) assert.match(targets, new RegExp(name));

  const renderGuard = read('build/Assert-InteractiveServerRenderModes.ps1');
  assert.match(renderGuard, /Components\/Pages\/Editor\.razor/);
  assert.match(renderGuard, /AddInteractiveServerRenderMode\(\)/);
  assert.match(renderGuard, /\(\?m\)\^\\s\*\$escapedDirective\\s\*\$/);
  assert.doesNotMatch(renderGuard, /\$first\s+-cne/);
  const asyncGuard = read('build/Assert-AsyncContinuationPolicy.ps1');
  assert.match(asyncGuard, /Renderer\/component continuations must use ConfigureAwait\(true\)/);
  assert.doesNotMatch(asyncGuard, /"[^"\r\n]*\$[A-Za-z_][A-Za-z0-9_]*:/);
  const componentGuard = read('build/Assert-ComponentDiagnostics.ps1');
  assert.match(componentGuard, /Dispose-only methods remain exempt/);
  const boundary = read('src/PublisherStudio.Web/Components/Shared/OperationalErrorBoundary.cs');
  assert.match(boundary, /exception is OperationCanceledException or TaskCanceledException or JSDisconnectedException/);
});
