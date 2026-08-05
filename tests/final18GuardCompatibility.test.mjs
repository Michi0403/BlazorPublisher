import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const shared = read('build/Invoke-ArchitectureAudit.ps1');
assert.match(shared, /Get-Command python/);
assert.match(shared, /Get-Command py/);
assert.match(shared, /using the bundled Windows PowerShell fallback audit/);
assert.match(shared, /function Invoke-StaticFallback/);
assert.match(shared, /function Invoke-MethodFallback/);
assert.match(shared, /function Invoke-RuntimeFallback/);

for (const [name, mode] of [
  ['Assert-MethodDiagnostics.ps1', 'methods'],
  ['Assert-ApplicationStaticPolicy.ps1', 'static'],
  ['Assert-RuntimeValueOwnership.ps1', 'runtime'],
]) {
  const guard = read(`build/${name}`);
  assert.match(guard, /Invoke-ArchitectureAudit\.ps1/);
  assert.match(guard, new RegExp(`-Mode ${mode}`));
}

for (const name of [
  'Assert-IteratorExceptionPolicy.ps1',
  'Assert-TextServiceOwnership.ps1',
  'Assert-SystemVariableInitialization.ps1',
]) {
  const guard = read(`build/${name}`);
  assert.match(guard, /\$parsedBaseline =/);
  assert.match(guard, /foreach \(\$item in \$parsedBaseline\)/);
  assert.doesNotMatch(guard, /\$baseline = @\(\[System\.IO\.File\]::ReadAllText/);
}

const oneWire = read('build/Assert-OneWireArchitecture.ps1');
assert.ok(oneWire.includes('systemVariables\\.DefaultPort'));
assert.match(oneWire, /SystemVariableStoreService\.cs/);
assert.ok(oneWire.includes('Application\\.DefaultPort'));
assert.doesNotMatch(oneWire, /DefaultPort = 58071/);

console.log('PASS consolidated architecture audit, PowerShell fallback and system-variable-owned 1-Wire port guard contracts.');
