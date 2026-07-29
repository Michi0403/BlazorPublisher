import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const required = [
  'PublisherStudio.sln',
  'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
  'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj',
  'Build-LocalDevelopment.cmd',
  'Build-LocalDevelopment.ps1',
  'Build-Release.ps1',
  'README.md'
];
for (const rel of required) assert.ok(fs.existsSync(path.join(root, rel)), `Required maintained source missing: ${rel}`);
assert.equal(fs.existsSync(path.join(root, 'src', 'LocalGPT.WireProtocolVersion')), false);
const packageFiles = fs.readdirSync(path.join(root, 'packages')).filter(name => name.endsWith('.nupkg')).sort();
assert.deepEqual(packageFiles, ['LocalGPT.WireProtocolVersion.2.1.0.nupkg'], 'Only the authoritative protocol package required for an offline source build may be shipped.');
assert.ok(fs.statSync(path.join(root, 'packages', packageFiles[0])).size > 0, 'The authoritative protocol package must not be empty.');
console.log('PASS current PublisherStudio maintained-source preservation contract.');
