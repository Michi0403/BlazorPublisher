import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('release documentation validation remains compatible with Windows PowerShell 5.1', () => {
  const release = read('Build-Release.ps1');
  assert.match(release, /\$index\.IndexOf\(\$marker, \[StringComparison\]::Ordinal\) -lt 0/);
  assert.doesNotMatch(release, /\.Contains\([^\r\n,]+,\s*\[(?:System\.)?StringComparison\]::/);

  const guard = read('build/Assert-PublishConfiguration.ps1');
  assert.match(guard, /Windows PowerShell 5\.1/);
});

test('DocFX home cards target source documents instead of generated html files', () => {
  const index = read('docs/index.md');
  for (const target of [
    'articles/getting-started.md',
    'articles/pictures-and-media.md',
    'articles/publishing-and-export.md',
    'articles/streaming-and-recording.md',
    'articles/localgpt-and-onewire.md',
    'api/index.md'
  ]) {
    assert.ok(index.includes(`href="${target}"`), target);
  }
  assert.doesNotMatch(index, /href="(?:articles\/(?:getting-started|pictures-and-media|publishing-and-export|streaming-and-recording|localgpt-and-onewire)|api\/index)\.html"/);
});
