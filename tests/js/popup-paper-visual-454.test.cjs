'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const docsJs = fs.readFileSync(path.join(root, 'docs/templates/publisherstudio/public/main.js'), 'utf8');
const docsCss = fs.readFileSync(path.join(root, 'docs/templates/publisherstudio/public/main.css'), 'utf8');
const appCss = fs.readFileSync(path.join(root, 'src/PublisherStudio.Web/wwwroot/css/site.css'), 'utf8');
for (const name of ['kawaiiTearOutline', 'kawaiiTearPaperLips', 'kawaiiTearFlaps', 'clearKawaiiTearSession']) {
  assert.ok(docsJs.includes(`function ${name}(`), `${name} missing`);
}
for (const name of ['tear-paper-lip','tear-flap-shadow','tear-fold','tear-fiber','cat-scratch','cursor-paw']) {
  assert.ok(docsCss.includes(`publisherstudio-${name}`), `${name} missing`);
}
assert.match(docsCss, /not\(\[data-bs-theme="dark"\]\) \.publisherstudio-tear-paper-lip/);
assert.match(docsJs, /session\.target\.style\.maskImage = url;/);
assert.match(docsJs, /session\.target\.style\.maskImage = session\.initialMask;/);
const scope = appCss.slice(appCss.indexOf('/* 4.5.4 — Popup surface ownership repair.'), appCss.indexOf('/* PublisherStudio 4.0.4: compatibility fallback'));
assert.ok(scope.includes(':has(> :is('), 'Popup selector must stay scoped to owned dialog roots.');
assert.ok(!scope.includes('dxbl-modal-body {'), 'No global popup body reset.');
assert.ok(!scope.includes('dxbl-popup-root .dxbl-modal-body {'), 'No broad popup body reset.');
console.log('PASS: scoped popup repair and paper-edge renderer are retained');
