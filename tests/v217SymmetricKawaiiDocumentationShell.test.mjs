import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '..');
const cssPath = path.join(root, 'docs/templates/publisherstudio/public/main.css');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('PublisherStudio and LocalGPT share the symmetric Kawaii desktop shell contract', () => {
  const css = fs.readFileSync(cssPath, 'utf8');
  for (const marker of [
    '--kawaii-docs-rail-width: clamp(15rem, 16vw, 18rem)',
    '--kawaii-docs-panel-gap: clamp(1.25rem, 2vw, 2.5rem)',
    '--kawaii-docs-shell-min-height:',
    'grid-template-columns:',
    'var(--kawaii-docs-rail-width)',
    'minmax(0, 1fr)',
    'column-gap: var(--kawaii-docs-panel-gap)',
    'margin-inline: auto !important',
    'max-width: 112rem !important',
    'width: calc(100% - clamp(2rem, 6vw, 6rem)) !important',
    'min-height: var(--kawaii-docs-shell-min-height) !important',
    'position: static !important',
    'grid-column: 1',
    'grid-column: 2',
    'grid-column: 3',
    'max-width: none !important',
    'overflow: visible !important',
    'publisherstudio-snapshot-layout' 
  ]) assert.ok(css.includes(marker), marker);

  assert.equal((css.match(/var\(--kawaii-docs-rail-width\)/g) ?? []).length >= 2, true);
  assert.doesNotMatch(css, /grid-template-columns:\s*[^;]*230px/i);
});

test('the tracked Pages snapshot carries the same layout CSS and current version', () => {
  const script = `
import hashlib, json, sys, zipfile
from pathlib import Path
archive=Path(sys.argv[1])
source=Path(sys.argv[2]).read_bytes()
with zipfile.ZipFile(archive) as z:
    css=z.read('styles/publisherstudio-kawaii.css')
    status=json.loads(z.read('documentation-status.json'))
    names=set(z.namelist())
    index=z.read('articles/getting-started.html').decode('utf-8')
assert css == source
assert status['version'] == '2.1.9'
assert status['pdfFileName'] == 'PublisherStudio-2.1.9.pdf'
assert 'PublisherStudio-2.1.9.pdf' in names
assert 'publisherstudio-snapshot-layout' in index
assert 'publisherstudio-snapshot-nav' in index
print(hashlib.sha256(css).hexdigest())
`;
  execFileSync('python', ['-c', script, path.join(root, '..', '.github/pages/publisherstudio-kawaii-docs.zip'), cssPath], { stdio: 'pipe' });
});

test('2.1.9 source version surfaces remain aligned', () => {
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>2\.1\.9<\/Version>/);
  assert.match(read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), /<Version>2\.1\.9<\/Version>/);
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package.json')).version, '2.1.9');
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package-lock.json')).version, '2.1.9');
});
