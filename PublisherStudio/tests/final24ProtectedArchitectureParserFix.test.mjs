import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');

test('final24 obsolete protected-architecture guard remains removed', () => {
  assert.equal(fs.existsSync(path.join(root, 'build', 'Assert-ProtectedArchitectureFiles.ps1')), false);
  assert.equal(fs.existsSync(path.join(root, 'build', 'protected-architecture-files.sha256')), false);
  const targets = fs.readFileSync(path.join(root, 'Directory.Build.targets'), 'utf8');
  assert.doesNotMatch(targets, /Assert-ProtectedArchitectureFiles\.ps1/);
  assert.doesNotMatch(targets, /protected-architecture-files\.sha256/);
});
