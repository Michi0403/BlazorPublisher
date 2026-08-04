import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');
const changelog = read('CHANGELOG-v2.0.2.md');
const ledger = read('docs','architecture','task-ledger.md');
const agents = read('AGENTS.md');
const release = read('RELEASE.md');
for (const status of ['Closed','Partial','Deferred']) {
  assert.ok(changelog.includes(status), `${status} missing from changelog`);
  assert.ok(ledger.includes(status), `${status} missing from task ledger`);
}
for (const task of ['visual OpenSCAD builder','Native OpenSCAD','operating-system-global','localization','static']) assert.match((changelog + ledger).toLowerCase(), new RegExp(task.toLowerCase().replace(/[.*+?^${}()|[\]\\]/g, '\\$&')));
assert.match(agents, /Release task ledger/);
assert.match(agents, /OpenSCAD builder compatibility/);
assert.match(agents, /private static methods/);
assert.match(release, /docs\/architecture\/task-ledger\.md/);
assert.match(release, /2\.0\.2/);
console.log('release changelog/task-ledger continuity and architecture-maintenance contracts passed');
