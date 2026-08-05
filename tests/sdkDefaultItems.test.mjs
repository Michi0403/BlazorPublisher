import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const projectPath = path.join(root, 'src', 'PublisherStudio.Web', 'PublisherStudio.Web.csproj');
const project = fs.readFileSync(projectPath, 'utf8');

// Microsoft.NET.Sdk.Web includes project-local content by default. Re-including
// Localization JSON files with Include produces NETSDK1022. Update attaches the
// copy metadata to the implicit Content items without replacing SDK defaults.
assert.doesNotMatch(project, /<EnableDefaultContentItems>\s*false\s*<\/EnableDefaultContentItems>/i);
assert.doesNotMatch(project, /<Content\s+Include="Localization\\/i);
assert.match(
  project,
  /<Content\s+Update="Localization\\\*\*\\\*\.json"\s+CopyToOutputDirectory="PreserveNewest"\s+CopyToPublishDirectory="PreserveNewest"\s*\/>/
);

for (const culture of ['de-DE', 'en-US', 'es-ES', 'ja-JP']) {
  const resource = path.join(root, 'src', 'PublisherStudio.Web', 'Localization', `${culture}.json`);
  assert.ok(fs.existsSync(resource), `Missing localization resource ${culture}.json`);
  assert.doesNotThrow(() => JSON.parse(fs.readFileSync(resource, 'utf8')), `${culture}.json must remain valid JSON`);
}

console.log('SDK implicit Content item and localization copy-metadata contract passed.');
