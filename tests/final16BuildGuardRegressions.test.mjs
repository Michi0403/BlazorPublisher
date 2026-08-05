import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('PowerShell 5.1 path normalization is centralized and wrappers use the shared audit', () => {
  const core = read('build/Invoke-ArchitectureAudit.ps1');
  assert.doesNotMatch(core, /TrimStart\('\\','\/'\)/);
  assert.match(core, /TrimStart\(\[char\[\]\]@\(\[char\]'\\', \[char\]'\/'\)\)/);
  assert.match(core, /Replace\('\\', '\/'\)/);

  for (const [name, mode] of [
    ['Assert-MethodDiagnostics.ps1', 'methods'],
    ['Assert-ApplicationStaticPolicy.ps1', 'static'],
    ['Assert-RuntimeValueOwnership.ps1', 'runtime'],
  ]) {
    const wrapper = read(`build/${name}`);
    assert.match(wrapper, /Invoke-ArchitectureAudit\.ps1/);
    assert.match(wrapper, new RegExp(`-Mode ${mode}`));
  }

  for (const name of ['Assert-TextServiceOwnership.ps1', 'Assert-IteratorExceptionPolicy.ps1', 'Assert-SystemVariableInitialization.ps1']) {
    const script = read(`build/${name}`);
    assert.doesNotMatch(script, /TrimStart\('\\','\/'\)/);
    assert.match(script, /TrimStart\(\[char\[\]\]@\(\[char\]'\\', \[char\]'\/'\)\)/);
    assert.match(script, /Replace\(\[char\]'\\', \[char\]'\/'\)/);
  }
});
