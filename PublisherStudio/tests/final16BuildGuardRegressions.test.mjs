import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('PowerShell 5.1 path normalization uses single characters in every new guard', () => {
  for (const name of [
    'Assert-MethodDiagnostics.ps1',
    'Assert-ApplicationStaticPolicy.ps1',
    'Assert-TextServiceOwnership.ps1',
    'Assert-IteratorExceptionPolicy.ps1'
  ]) {
    const script = read(path.join('build', name));
    assert.doesNotMatch(script, /TrimStart\('\\\\','\/'\)/);
    assert.match(script, /TrimStart\(\[char\[\]\]@\(\[char\]'\\', \[char\]'\/'\)\)/);
    assert.match(script, /Replace\(\[char\]'\\', \[char\]'\/'\)/);
  }
});

test('logging integrity ignores extracted build workspaces', () => {
  const script = read('build/Assert-LoggingIntegrity.ps1');
  assert.match(script, /bin\|obj\|artifacts\|node_modules\|build/);
});
