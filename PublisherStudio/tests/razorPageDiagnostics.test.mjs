import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', 'src', 'PublisherStudio.Web', 'Components', 'Pages');
for (const name of fs.readdirSync(root).filter(name => name.endsWith('.razor'))) {
  const component = path.basename(name, '.razor');
  const source = fs.readFileSync(path.join(root, name), 'utf8');
  assert.match(source, new RegExp(`@inject\\s+ILogger<${component}>\\s+Logger`), `${name} must inject its typed logger`);
  assert.match(source, /@inject\s+IUserNotificationService\s+Notifications/, `${name} must inject the user notifier`);
}
console.log('PublisherStudio Razor page logger and notifier contracts passed');
