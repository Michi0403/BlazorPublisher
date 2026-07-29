import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const testsRoot = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(testsRoot, '..');
const buildRoot = path.join(repositoryRoot, 'build');

const read = relative => fs.readFileSync(path.join(repositoryRoot, relative), 'utf8');

test('architecture policies use zero debt baselines and preserve legal bootstrap boundaries', () => {
  assert.deepEqual(JSON.parse(read('build/application-static-baseline.json')), []);
  assert.deepEqual(JSON.parse(read('build/method-diagnostics-baseline.json')), []);
  assert.deepEqual(JSON.parse(read('build/runtime-value-ownership-baseline.json')), []);

  const policy = read('docs/SAFE_STATIC_RUNTIME_AND_DIAGNOSTICS_POLICY.md');
  assert.match(policy, /Program\.cs/);
  assert.match(policy, /P\/Invoke and native exports belong behind injected lifetime services/);
  assert.match(policy, /Records, DTOs, constructors/);

  const extension = read('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs');
  assert.match(extension, /this IServiceCollection services, IConfiguration configuration, ILogger logger/);
  assert.match(extension, /try\s*\{/);
  assert.match(extension, /catch \(Exception exception\)/);
});

test('Python architecture audit passes when Python is available', t => {
  const script = path.join(buildRoot, 'audit_application_architecture.py');
  const candidates = process.platform === 'win32'
    ? [['py', ['-3']], ['python', []]]
    : [['python3', []], ['python', []]];

  for (const [command, prefix] of candidates) {
    const probe = spawnSync(command, [...prefix, '--version'], { encoding: 'utf8' });
    if (probe.status !== 0) continue;
    const result = spawnSync(command, [...prefix, script, '--root', repositoryRoot, '--product', 'publisherstudio', '--mode', 'all'], { encoding: 'utf8' });
    assert.equal(result.status, 0, `${result.stdout}\n${result.stderr}`);
    return;
  }
  t.skip('Python is unavailable; the PowerShell fallback remains the build-time enforcement path.');
});
