import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const servicePath = path.join(
  root,
  'src',
  'PublisherStudio.Web',
  'Services',
  'VideoStudio',
  'Export',
  'BrowserRuntimeTemplateService.cs'
);
const source = fs.readFileSync(servicePath, 'utf8');

// JavaScript contains ordinary braces, adjacent closing braces, and template
// expressions such as ${...}. It therefore must not be embedded in a C#
// interpolated raw string whose brace delimiter can collide with that syntax.
assert.doesNotMatch(source, /CreateBlobRuntime\([^)]*\)\s*=>\s*\$+"""/);
assert.match(source, /CreateBlobRuntime\([^)]*\)\s*=>\s*"""/);
assert.match(source, /__PUBLISHERSTUDIO_BLOB_RUNTIME_PAYLOAD__/);
assert.match(source, /\.Replace\([\s\S]*StringComparison\.Ordinal\s*\)/);
assert.match(source, /`rgba\(2,20,42,\$\{/);

const rawTemplate = source.match(/=>\s*"""\r?\n([\s\S]*?)\r?\n"""\.Replace\(/);
assert.ok(rawTemplate, 'Browser runtime raw template could not be extracted.');

const payload = JSON.stringify({
  source: [[0, 0], [1, 0], [0.5, 1]],
  target: [[0.1, 0.1], [0.9, 0.1], [0.5, 0.9]],
  animate: true,
  morphEnabled: true,
  speed: 1,
  morphAmount: 0.5,
  depth: 0.25,
  opacity: 1
});
const runtime = rawTemplate[1].replace('__PUBLISHERSTUDIO_BLOB_RUNTIME_PAYLOAD__', payload);
assert.doesNotMatch(runtime, /__PUBLISHERSTUDIO_BLOB_RUNTIME_PAYLOAD__/);
assert.match(runtime, /const config = \{"source":/);

const tempFile = path.join(os.tmpdir(), `publisher-runtime-${process.pid}.js`);
try {
  fs.writeFileSync(tempFile, runtime, 'utf8');
  const check = spawnSync(process.execPath, ['--check', tempFile], { encoding: 'utf8' });
  assert.equal(check.status, 0, check.stderr || check.stdout || 'Generated JavaScript failed syntax validation.');
} finally {
  fs.rmSync(tempFile, { force: true });
}

console.log('Browser runtime C# raw-string and generated JavaScript safety contract passed.');
