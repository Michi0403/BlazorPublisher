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
assert.equal(fs.readdirSync(path.join(root, 'packages')).some(name => name.endsWith('.nupkg')), false, 'Generated protocol packages must not be committed.');
console.log('PASS current PublisherStudio maintained-source preservation contract.');
