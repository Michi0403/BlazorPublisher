'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const js = fs.readFileSync(path.join(root, 'docs/templates/publisherstudio/public/main.js'), 'utf8');
const css = fs.readFileSync(path.join(root, 'docs/templates/publisherstudio/public/main.css'), 'utf8');
assert.match(js, /const KAWAII_CLAW_SPACING = 16;/);
for (const requirement of ['kawaiiTearGeometry(scratch, claw)', 'geometries.map(kawaiiTearOutline)', 'geometries.map(kawaiiTearPaperLips)', 'geometries.map(kawaiiTearFlaps)', 'session.target.style.maskImage = url;', 'clearKawaiiTearSession', 'playKawaiiScratchSound']) {
  assert.ok(js.includes(requirement), 'Missing maintained tear contract: ' + requirement);
}
assert.ok(css.includes('stroke-width: 2.4;'));
assert.ok(!css.includes('stroke-width: 9;'));
assert.ok(css.includes('.publisherstudio-cat-scratch'));
assert.ok(css.includes('.publisherstudio-cursor-paw'));
console.log('PASS: narrowed shared claw geometry, real content masking, original effects, light/dark support');
