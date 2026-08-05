import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const web = path.join(root, 'src', 'PublisherStudio.Web');

const csharpFiles = [];
function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === 'bin' || entry.name === 'obj') continue;
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(full);
    else if (entry.isFile() && entry.name.endsWith('.cs')) csharpFiles.push(full);
  }
}
walk(web);

function stripComments(text) {
  return text
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/(^|[^:])\/\/.*$/gm, '$1');
}

function namespaceOf(text) {
  const match = text.match(/^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]/m);
  return match?.[1] ?? '(global)';
}

const declarations = [];
const declarationPattern = /(?:^|\n)\s*(?:\[[^\]]+\]\s*)*(?:public|internal)\s+(?:(?:sealed|abstract|static|partial|readonly|ref|unsafe)\s+)*(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+([A-Za-z_][A-Za-z0-9_]*)/g;

for (const file of csharpFiles) {
  const text = stripComments(fs.readFileSync(file, 'utf8'));
  const namespaceName = namespaceOf(text);
  for (const match of text.matchAll(declarationPattern)) {
    declarations.push({
      name: match[1],
      namespaceName,
      file: path.relative(root, file)
    });
  }
}

const byName = new Map();
for (const declaration of declarations) {
  const entries = byName.get(declaration.name) ?? [];
  entries.push(declaration);
  byName.set(declaration.name, entries);
}

// BusinessObjects own shared contracts. Services must consume those contracts rather
// than introducing an identically named shadow type in an implementation namespace.
for (const [name, entries] of byName) {
  const shared = entries.filter(entry => /^PublisherStudio\.BusinessObjects(?:\.|$)/.test(entry.namespaceName));
  const services = entries.filter(entry => /^PublisherStudio\.Services(?:\.|$)/.test(entry.namespaceName));
  assert.equal(
    shared.length > 0 && services.length > 0,
    false,
    `Service type shadows a BusinessObjects contract named ${name}: ${entries.map(entry => `${entry.namespaceName} (${entry.file})`).join(', ')}`
  );
}

// Every namespace imported globally contributes to one project-wide simple-name
// scope. Reject collisions before Visual Studio has to report CS0104.
const globallyImportedNamespaces = new Set();
for (const file of csharpFiles.filter(file => /^GlobalUsings.*\.cs$/i.test(path.basename(file)))) {
  const text = stripComments(fs.readFileSync(file, 'utf8'));
  for (const match of text.matchAll(/^\s*global\s+using\s+(?!static\b)(?![A-Za-z_]\w*\s*=)([A-Za-z_][A-Za-z0-9_.]*)\s*;/gm))
    globallyImportedNamespaces.add(match[1]);
}

for (const [name, entries] of byName) {
  const visible = entries.filter(entry => globallyImportedNamespaces.has(entry.namespaceName));
  const namespaces = [...new Set(visible.map(entry => entry.namespaceName))];
  assert.ok(
    namespaces.length <= 1,
    `Global-using type collision for ${name}: ${visible.map(entry => `${entry.namespaceName} (${entry.file})`).join(', ')}`
  );
}

const hotkeyDeclarations = byName.get('MediaHostHotkeyEvent') ?? [];
assert.deepEqual(
  hotkeyDeclarations.map(entry => entry.namespaceName),
  ['PublisherStudio.BusinessObjects.Streaming'],
  'MediaHostHotkeyEvent must have exactly one authoritative declaration in BusinessObjects/Streaming.'
);

const mediaHostClient = fs.readFileSync(path.join(web, 'Services', 'Streaming', 'MediaHost', 'StreamingMediaHostClient.cs'), 'utf8');
assert.match(mediaHostClient, /Task<IReadOnlyList<MediaHostHotkeyEvent>>\s+ReadEventsAsync/);
assert.match(mediaHostClient, /Task\.FromResult\(_sessions\.DrainEvents\(sessionId\)\)/);
assert.doesNotMatch(mediaHostClient, /(?:class|record)\s+MediaHostHotkeyEvent\b/);
assert.doesNotMatch(mediaHostClient, /new\s+MediaHostHotkeyEvent\s*\{/);

console.log('PublisherStudio C# shared-contract ownership checks passed.');
