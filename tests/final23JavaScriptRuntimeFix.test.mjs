import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const normalizedHash = relative => crypto.createHash('sha256').update(read(relative).replace(/\r\n?/g, '\n'), 'utf8').digest('hex');
const loadManifest = relative => new Map(read(relative).split(/\r?\n/).map(line => line.trim()).filter(line => line && !line.startsWith('#')).map(line => {
  const match = /^([0-9a-f]{64})\s{2}(.+)$/.exec(line);
  assert.ok(match, `invalid manifest line: ${line}`);
  return [match[2].replaceAll('\\', '/'), match[1]];
}));

test('final23 browser diagnostics load early and mirror JavaScript failures to ILogger', () => {
  const app = read('src/PublisherStudio.Web/Components/App.razor');
  const diagnostics = app.indexOf('<script src="js/javascript-diagnostics.js"></script>');
  assert.ok(diagnostics >= 0 && diagnostics < app.indexOf('<script src="vendor/jquery/jquery.min.js"></script>'));
  assert.ok(diagnostics < app.indexOf('<script src="_framework/blazor.web.js"></script>'));
  assert.match(app, /<JavaScriptDiagnosticsBridge \/>/);
  const runtime = read('src/PublisherStudio.Web/wwwroot/js/javascript-diagnostics.js');
  for (const token of ['console.error', 'window.addEventListener("error"', 'unhandledrejection', 'ReportJavaScriptErrorAsync', 'pendingReports', 'guardObject', 'guardClass']) assert.ok(runtime.includes(token));
  const bridge = read('src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor');
  for (const token of ['InteractiveServerRenderMode(prerender: false)', 'publisherStudioJavaScriptDiagnostics.bindDotNet', '[JSInvokable]', 'Logger.LogError']) assert.ok(bridge.includes(token));
});

test('final23 preserves stable Panel Studio binding and guards every reviewed browser file', () => {
  const lifecycle = read('build/Assert-PanelStudioInteractionLifecycle.ps1');
  assert.match(lifecycle, /bindPanelStudioDropSurface\\\(element, dotNetReference, bindingId = ''\\\)/);
  const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
  assert.match(interop, /export function bindPanelStudioDropSurface\(element, dotNetReference, bindingId = ''\)\s*\{\s*try\s*\{/);
  const manifest = loadManifest('build/javascript-diagnostics-files.sha256');
  const jsRoot = path.join(root, 'src/PublisherStudio.Web/wwwroot/js');
  const files = fs.readdirSync(jsRoot).filter(name => name.endsWith('.js')).map(name => `src/PublisherStudio.Web/wwwroot/js/${name}`).sort();
  assert.deepEqual([...manifest.keys()].sort(), files);
  for (const relative of files) {
    const text = read(relative);
    assert.equal(normalizedHash(relative), manifest.get(relative), relative);
    assert.match(text, /javascript-diagnostics:\s*guarded/, relative);
    assert.match(text, /\btry\s*\{/, relative);
    assert.match(text, /\bcatch\s*(?:\([^)]*\))?\s*\{/, relative);
    assert.doesNotMatch(text, /catch\s*(?:\([^)]*\))?\s*\{\s*\}/, relative);
  }
  const guard = read('build/Assert-JavaScriptDiagnostics.ps1');
  const targets = read('Directory.Build.targets');
  assert.match(guard, /javascript-diagnostics-files\.sha256/);
  assert.match(targets, /Assert-JavaScriptDiagnostics\.ps1/);
  assert.match(targets, /AssertPublisherJavaScriptDiagnostics/);
});
