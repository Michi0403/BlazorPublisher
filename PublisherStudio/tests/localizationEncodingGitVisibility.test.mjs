import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const marker = '\u2420';

test('localization catalogs and required German strings remain intact', () => {
  const en = JSON.parse(read('src/PublisherStudio.Web/Localization/en-US.json'));
  const de = JSON.parse(read('src/PublisherStudio.Web/Localization/de-DE.json'));
  assert.deepEqual(Object.keys(en).sort(), Object.keys(de).sort());
  assert.equal(de[`Text.Panel${marker}/${marker}Div${marker}Studio`], 'Panel-/DIV-Studio');
  assert.equal(de[`Text.Save${marker}panel`], 'Panel speichern');
});

test('PowerShell gates remain ASCII-only and reconstruct U+2420 at runtime', () => {
  for (const relative of ['build/Assert-LocalizationIntegrity.ps1', 'build/Assert-GitSourceVisibility.ps1']) {
    const bytes = fs.readFileSync(path.join(root, relative));
    assert.ok([...bytes].every(byte => byte < 128), `${relative} must remain ASCII-only`);
  }
  const guard = read('build/Assert-LocalizationIntegrity.ps1');
  assert.match(guard, /\[char\]0x2420/);
  assert.match(guard, /System\.Text\.UTF8Encoding/);
  assert.match(guard, /ReadAllText/);
  assert.ok(!guard.includes(marker));
  assert.ok(!guard.includes('â'));
});

test('MSBuild, build scripts and gitignore protect the repaired gates', () => {
  const targets = read('Directory.Build.targets');
  assert.match(targets, /AssertPublisherGitSourceVisibility/);
  assert.match(targets, /SkipGitSourceVisibilityGuard/);
  for (const relative of ['Build-LocalDevelopment.ps1', 'Build-Release.ps1']) {
    const content = read(relative);
    assert.match(content, /Assert-LocalizationIntegrity\.ps1/);
    assert.match(content, /Assert-GitSourceVisibility\.ps1/);
    assert.match(content, /SkipGitSourceVisibilityGuard=true/);
  }
  const ignore = read('.gitignore');
  for (const rule of ['!build/*.ps1', '!tests/*.mjs', '!src/PublisherStudio.Web/Localization/*.json']) {
    assert.ok(ignore.includes(rule));
  }
});
