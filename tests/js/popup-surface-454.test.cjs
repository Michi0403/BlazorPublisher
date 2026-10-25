'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
const popup = css.slice(css.indexOf('/* 4.5.4 — Popup surface ownership repair.'), css.indexOf('/* PublisherStudio 4.0.4: compatibility fallback'));
assert.ok(popup.length > 500, 'Popup ownership repair must be maintained with its scope and rationale.');
assert.match(popup, /\.dxbl-modal-body:has\(> :is\(/, 'No blanket popup-body reset.');
assert.match(popup, /\.dxbl-modal-content:has\(> \.dxbl-modal-body > :is\(/, 'Popup shell must not introduce a second inset.');
for (const dialog of ['publisher-dialog', 'story-editor-dialog', 'picture-studio-dialog', 'media-studio-window', 'panel-studio-dialog', 'new-publication-dialog', 'publisher-documentation-dialog']) {
  assert.ok(popup.includes('.' + dialog), `Owned dialog ${dialog} must be included`);
}
assert.match(popup, /padding: 0 !important/);
assert.match(popup, /scrollbar-gutter: auto !important/);
assert.match(popup, /\.dxbl-modal-body > \.publisher-dialog\s*\{[^}]*width: 100% !important/);
assert.match(popup, /\.dxbl-modal-body > \.publisher-dialog\s*\{[^}]*border: 0/);
assert.match(editor, /<DxPopup Visible="true" Width="min\(470px, 94vw\)" ShowHeader="false"/);
assert.match(editor, /<div class="publisher-dialog" aria-label='@LT\("Reconnect to LocalGPT\?"\)'/);
assert.match(editor, /Click="DeclineLocalGptReconnectAsync"/);
assert.match(editor, /Click="ApproveLocalGptReconnectAsync"/);
assert.match(css, /\.publisher-dialog > header\s*\{[^}]*background: linear-gradient/);
assert.match(css, /\.publisher-dialog > footer\s*\{[^}]*justify-content: flex-end/);
assert.match(css, /\.publisher-ai-dialog > footer\s*\{[^}]*position: sticky/);
for (const proj of ['src/PublisherStudio.Web/PublisherStudio.Web.csproj', 'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']) {
  assert.match(read(proj), /<Version>4\.5\.7<\/Version>/);
}
console.log('PASS: 4.5.4 scoped popup frame/fill contract, actions and maintenance-safe versioning');
