'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = name => fs.readFileSync(path.join(root, name), 'utf8');
const script = read('docs/templates/publisherstudio/public/main.js');
const css = read('docs/templates/publisherstudio/public/main.css');
for (const project of ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']) {
  assert.match(read(project), /<Version>4\.5\.7<\/Version>/);
}
for (const marker of ['function makeKawaiiGalaxyPaw()', 'class="publisherstudio-paw-fur"', 'class="publisherstudio-paw-fur-detail"', 'session.target.style.maskImage = url;', 'const KAWAII_CLAW_SPACING = 16;', 'playKawaiiScratchSound']) {
  assert.ok(script.includes(marker), marker);
}
assert.ok(css.includes('drop-shadow(0 0 3px rgba(75, 225, 255, .90))'));
assert.ok(css.includes(':not([data-bs-theme="dark"]) .publisherstudio-cursor-paw'));
assert.ok(css.includes('drop-shadow(0 0 5px rgba(59, 44, 82, .82))'));
assert.ok(css.includes('transform: translate(-.35rem, -.55rem)'));
assert.ok(css.includes('url(#publisherstudio-tear-space-light)'));
console.log('PASS: paw silhouette, visible dark-mode rim, dark light-mode halo and glowing light-theme tears, and scratch preservation');
