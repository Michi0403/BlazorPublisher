import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const normalizedHash = relative => crypto.createHash('sha256').update(read(relative).replace(/\r\n?/g, '\n'), 'utf8').digest('hex');

test('final24 protected architecture array is valid for Windows PowerShell 5.1', () => {
  const script = read('build/Assert-ProtectedArchitectureFiles.ps1');
  assert.match(script, /'src\/PublisherStudio\.Web\/appsettings\.json',\s*\n\s*'build\/Assert-JavaScriptDiagnostics\.ps1',/);
  assert.match(script, /'src\/PublisherStudio\.Web\/Components\/Layout\/JavaScriptDiagnosticsBridge\.razor',\s*\n\s*'build\/Update-ReviewedProtectionManifest\.ps1',/);
  const expectedArray = script.match(/\$expectedFiles\s*=\s*@\(([\s\S]*?)\n\)/)[1].trimEnd();
  assert.doesNotMatch(expectedArray, /,\s*$/);
  assert.match(script, /'tests\/final27ReviewedManifestRefresh\.test\.mjs'/);
});

test('final24 protected architecture manifest matches the repaired script', () => {
  const manifest = read('build/protected-architecture-files.sha256');
  const match = new RegExp(`^${normalizedHash('build/Assert-ProtectedArchitectureFiles.ps1')}  build/Assert-ProtectedArchitectureFiles\\.ps1$`, 'm');
  assert.match(manifest, match);
});
