import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const installer = fs.readFileSync(path.join(root, 'src/PublisherStudio.InstallerConsole/Program.cs'), 'utf8');
const docs = fs.readFileSync(path.join(root, 'build/Build-Documentation.ps1'), 'utf8');

test('2.1.8 uses direct exact-asset downloads with strict API fallback', () => {
  assert.match(installer, /releases\/latest\/download\/\{Uri\.EscapeDataString\(expectedAssetName\)\}/);
  assert.match(installer, /Falling back to the GitHub release API/);
  assert.match(installer, /The latest PublisherStudio release does not contain required asset/);
});

test('explicit archives are preserved and both archives validate before extraction', () => {
  assert.match(installer, /Using explicitly supplied PublisherStudio release archive/);
  const appValidation = installer.indexOf('ValidateReleaseArchive(zipPath');
  const setupValidation = installer.indexOf('ValidateReleaseArchive(setupZipPath');
  const appExtraction = installer.indexOf('ExtractZipWithFallback(zipPath');
  assert.ok(appValidation >= 0 && setupValidation > appValidation && appExtraction > setupValidation);
});

test('documentation source rewriting is idempotent', () => {
  assert.match(docs, /function Set-Utf8TextFileIdempotent/);
  assert.match(docs, /TrimEnd\("`r", "`n"\) \+ \[Environment\]::NewLine/);
  assert.doesNotMatch(docs, /Set-Content -LiteralPath \$indexPath -Value \$index/);
});
