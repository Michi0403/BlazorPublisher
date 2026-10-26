"use strict";
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const js = fs.readFileSync(path.join(root,'docs/templates/publisherstudio/public/main.js'),'utf8');
const css = fs.readFileSync(path.join(root,'docs/templates/publisherstudio/public/main.css'),'utf8');
for (const marker of [
  'function clippedMermaidNodeLabels(block)', 'getBoundingClientRect()',
  "const renderedBlocks = [...document.querySelectorAll('.mermaid')]",
  "window.matchMedia('(min-width: 768px)').matches",
  'htmlLabels: true, useMaxWidth: false',
  'fitAlreadyRenderedMermaidLabels(block)',
  "block.setAttribute('data-processed', 'true');",
  'installKawaiiTouchScratchToggle()',
  "event.pointerType === 'touch'",
  'moveKawaiiTouchPaw(event.clientX, event.clientY)',
  "if (event.pointerType !== 'touch' && (event.buttons & 1) === 0) return;",
  'session.target.style.maskImage = url;',
  'session.target.style.maskImage = session.initialMask;'
]) assert.ok(js.includes(marker), marker);
for (const marker of ['@media (any-pointer: coarse)', 'touch-action: none;', 'publisherstudio-touch-scratch-toggle', 'publisherstudio-touch-paw'])
  assert.ok(css.includes(marker), marker);
assert.ok(!css.includes('@media (prefers-reduced-motion: reduce), (pointer: coarse) {\n  html.publisherstudio-kawaii-docs .publisherstudio-tear-overlay'));
assert.match(fs.readFileSync(path.join(root,'src/PublisherStudio.Web/PublisherStudio.Web.csproj'),'utf8'),/<Version>\d+\.\d+\.\d+<\/Version>/);
console.log('PASS: responsive Mermaid node bounds/recovery and mobile touch mode contracts');
