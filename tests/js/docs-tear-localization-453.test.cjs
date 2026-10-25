'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root,file),'utf8');
const js = read('docs/templates/publisherstudio/public/main.js');
const css = read('docs/templates/publisherstudio/public/main.css');
assert.match(js, /installKawaiiTearInteraction\(\)/);
assert.match(js, /document\.querySelector\('main\.container-xxl > \.content'\)/);
assert.match(js, /session\.target\.style\.maskImage = url/);
assert.match(js, /session\.target\.style\.maskImage = session\.initialMask/);
assert.match(js, /'pagehide', clearKawaiiTearSession/);
assert.match(js, /'hashchange', clearKawaiiTearSession/);
assert.match(js, /playKawaiiScratchSound/);
assert.match(js, /event\.shiftKey/);
assert.match(js, /addKawaiiClick/);
assert.match(js, /makeKawaiiGalaxyPaw/);
assert.match(css, /publisherstudio-tear-paper/);
assert.match(css, /:not\(\[data-bs-theme="dark"\]\)/);
for (const culture of ['en-US','de-DE','es-ES','fr-FR','ja-JP','uk-UA']) {
  const catalog = JSON.parse(read(`src/PublisherStudio.Web/Localization/${culture}.json`));
  assert.ok(catalog['Phrase.Apply␠animated␠GIF'], `missing GIF translation: ${culture}`);
}
assert.equal(JSON.parse(read('src/PublisherStudio.Web/Localization/en-US.json'))['Phrase.Apply␠animated␠GIF'],'Apply animated GIF');
assert.equal(JSON.parse(read('src/PublisherStudio.Web/Localization/de-DE.json'))['Phrase.Apply␠animated␠GIF'],'Animiertes GIF anwenden');
for (const f of ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']) {
  assert.match(read(f), /<Version>4\.5\.3<\/Version>/);
}
console.log('PASS: 4.5.3 localization and transient DocFX tear contracts');
