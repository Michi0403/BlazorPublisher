import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

for (const name of [
  'Assert-MethodDiagnostics.ps1',
  'Assert-IteratorExceptionPolicy.ps1',
  'Assert-ApplicationStaticPolicy.ps1',
  'Assert-TextServiceOwnership.ps1',
  'Assert-SystemVariableInitialization.ps1'
]) {
  const guard = read(`build/${name}`);
  assert.match(guard, /\$parsedBaseline =/);
  assert.match(guard, /foreach \(\$item in \$parsedBaseline\)/);
  assert.doesNotMatch(guard, /\$baseline = @\(\[System\.IO\.File\]::ReadAllText/);
}

const oneWire = read('build/Assert-OneWireArchitecture.ps1');
assert.match(oneWire, /systemVariables\\?\.DefaultPort/);
assert.match(oneWire, /SystemVariableStoreService\.cs/);
assert.match(oneWire, /Application\\?\.DefaultPort/);
assert.doesNotMatch(oneWire, /DefaultPort = 58071/);


console.log('PASS final18 PowerShell baseline enumeration and system-variable-owned 1-Wire port guard contracts.');
