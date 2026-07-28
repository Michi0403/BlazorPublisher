import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

for (const name of ['Assert-MethodDiagnostics.ps1', 'Assert-IteratorExceptionPolicy.ps1']) {
  const guard = read(`build/${name}`);
  assert.doesNotMatch(guard, /System\.Collections\.Generic\.List\[object\]/);
  assert.doesNotMatch(guard, /return @\(\$records\)/);
  assert.match(guard, /\$records = @\(\)/);
  assert.match(guard, /return \$records/);
}

const program = read('src/PublisherStudio.Web/Program.cs');
const store = read('src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs');
const contract = read('src/PublisherStudio.Web/Services/Configuration/ISystemVariableStoreService.cs');
const host = read('src/PublisherStudio.Web/Services/ApplicationHostServices.cs');
const editor = read('src/PublisherStudio.Web/Services/EditorStateService.cs');
const targets = read('Directory.Build.targets');
const policy = read('build/Assert-SystemVariableInitialization.ps1');
const settings = JSON.parse(read('src/PublisherStudio.Web/appsettings.json'));

assert.match(program, /new SystemVariableStoreService\(builder\.Configuration\)/);
assert.match(program, /AddSingleton<ISystemVariableStoreService>\(systemVariables\)/);
assert.match(program, /systemVariables\.AttachLogger/);
assert.match(program, /systemVariables\.DefaultCulture/);
assert.match(program, /systemVariables\.CorsPolicyName/);
assert.match(host, /ISystemVariableStoreService systemVariables/);
assert.match(host, /systemVariables\.DefaultPort/);
assert.match(host, /systemVariables\.RuntimeEndpointFileName/);
assert.match(editor, /_systemVariables\.DefaultDocumentName/);
assert.match(editor, /_systemVariables\.DefaultCulture/);
assert.match(contract, /IReadOnlyDictionary<string, string> Snapshot\(\)/);
assert.match(store, /system-variables\.json/);
assert.match(store, /File\.Move\(temporaryPath, _storagePath, true\)/);
assert.match(targets, /AssertPublisherSystemVariableInitialization/);
assert.match(policy, /direct-system-variable-name/);
assert.ok(Array.isArray(JSON.parse(read('build/system-variable-initialization-baseline.json'))));
assert.equal(settings.PublisherStudio.SystemVariables['Application.DefaultPort'], '58071');
assert.equal(settings.PublisherStudio.SystemVariables['Application.DefaultCulture'], 'en-US');

console.log('PublisherStudio singleton system-variable store, initialization guard, and PowerShell 5.1 contracts passed.');
