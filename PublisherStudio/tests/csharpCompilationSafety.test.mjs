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

// This is intentionally a lexical guard, not a C# parser. It removes the common
// literal forms so namespace/type checks do not trigger on messages or comments.
function stripLiterals(text) {
  return text
    .replace(/\$?@"(?:""|[^"])*"/gs, '""')
    .replace(/\$?"""[\s\S]*?"""/g, '""')
    .replace(/\$?"(?:\\.|[^"\\])*"/g, '""')
    .replace(/'(?:\\.|[^'\\])'/g, "''");
}

function namespaceOf(text) {
  return text.match(/^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]/m)?.[1] ?? '(global)';
}

function normalizeNamespace(value) {
  return value.replace(/^global::/, '');
}

function importsOf(text) {
  const imports = new Set();
  const aliases = new Map();
  for (const match of text.matchAll(/^\s*(?:global\s+)?using\s+(?!static\b)(?:([A-Za-z_]\w*)\s*=\s*)?(global::)?([A-Za-z_][A-Za-z0-9_.]*)\s*;/gm)) {
    const namespaceName = normalizeNamespace(`${match[2] ?? ''}${match[3]}`);
    if (match[1]) aliases.set(match[1], namespaceName);
    else imports.add(namespaceName);
  }
  return { imports, aliases };
}

const globalImports = new Set();
for (const file of csharpFiles.filter(file => /^GlobalUsings.*\.cs$/i.test(path.basename(file)))) {
  const text = stripComments(fs.readFileSync(file, 'utf8'));
  for (const match of text.matchAll(/^\s*global\s+using\s+(?!static\b)(?![A-Za-z_]\w*\s*=)(?:global::)?([A-Za-z_][A-Za-z0-9_.]*)\s*;/gm))
    globalImports.add(match[1]);
}

const declarations = new Map();
const namespaces = new Set();
const declarationPattern = /(?:^|\n)\s*(?:\[[^\]]+\]\s*)*(?:public|internal)\s+(?:(?:sealed|abstract|static|partial|readonly|ref|unsafe)\s+)*(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+([A-Za-z_][A-Za-z0-9_]*)/g;
for (const file of csharpFiles) {
  const text = stripComments(fs.readFileSync(file, 'utf8'));
  const namespaceName = namespaceOf(text);
  namespaces.add(namespaceName);
  for (const match of text.matchAll(declarationPattern)) {
    const entries = declarations.get(match[1]) ?? [];
    entries.push({ namespaceName, file });
    declarations.set(match[1], entries);
  }
}

// Composition-root registrations are compile-time references. A moved HostedService
// or Service must bring its namespace import with it in the same change.
const compositionFiles = csharpFiles.filter(file =>
  path.basename(file) === 'Program.cs' || /ServiceCollectionExtensions\.cs$/i.test(path.basename(file))
);
const registrationPattern = /\b(?:Add|TryAdd)(?:Singleton|Scoped|Transient|HostedService)\s*<\s*([A-Za-z_][A-Za-z0-9_]*)\b/g;
for (const file of compositionFiles) {
  const raw = fs.readFileSync(file, 'utf8');
  const text = stripComments(raw);
  const currentNamespace = namespaceOf(text);
  const { imports, aliases } = importsOf(text);
  for (const match of text.matchAll(registrationPattern)) {
    const simpleName = match[1];
    const candidates = declarations.get(simpleName) ?? [];
    if (candidates.length !== 1) continue; // Framework/open-generic or intentionally ambiguous types are outside this lexical check.
    const targetNamespace = candidates[0].namespaceName;
    const visible = targetNamespace === currentNamespace
      || imports.has(targetNamespace)
      || globalImports.has(targetNamespace)
      || aliases.has(simpleName)
      || raw.includes(`${targetNamespace}.${simpleName}`)
      || raw.includes(`global::${targetNamespace}.${simpleName}`);
    assert.ok(
      visible,
      `Composition-root registration ${simpleName} in ${path.relative(root, file)} is missing an import/qualification for ${targetNamespace}.`
    );
  }
}

// C# resolves namespace members while walking enclosing namespaces. Therefore a
// sibling namespace named Encoding can shadow System.Text.Encoding in Chat/Lan.
// Guard common framework identifiers whenever the project contains a conflicting
// namespace leaf under an enclosing namespace. Existing collisions must use a
// global:: qualification or an explicit alias.
const frameworkTypes = new Map([
  ['Encoding', 'System.Text.Encoding'],
  ['Path', 'System.IO.Path'],
  ['File', 'System.IO.File'],
  ['Directory', 'System.IO.Directory'],
  ['Stream', 'System.IO.Stream'],
  ['Task', 'System.Threading.Tasks.Task'],
  ['Timer', 'System.Threading.Timer'],
  ['Environment', 'System.Environment'],
  ['Console', 'System.Console'],
  ['Convert', 'System.Convert'],
  ['Math', 'System.Math'],
  ['Random', 'System.Random'],
  ['Uri', 'System.Uri'],
  ['Version', 'System.Version'],
  ['Type', 'System.Type'],
  ['Guid', 'System.Guid'],
  ['DateTime', 'System.DateTime'],
  ['DateTimeOffset', 'System.DateTimeOffset'],
  ['TimeSpan', 'System.TimeSpan']
]);

const namespaceParentsByLeaf = new Map();
for (const namespaceName of namespaces) {
  if (namespaceName === '(global)') continue;
  const parts = namespaceName.split('.');
  if (parts.length < 2) continue;
  const leaf = parts.at(-1);
  const parents = namespaceParentsByLeaf.get(leaf) ?? new Set();
  parents.add(parts.slice(0, -1).join('.'));
  namespaceParentsByLeaf.set(leaf, parents);
}

for (const file of csharpFiles) {
  const raw = fs.readFileSync(file, 'utf8');
  const commentsRemoved = stripComments(raw);
  const currentNamespace = namespaceOf(commentsRemoved);
  const { aliases } = importsOf(commentsRemoved);
  const body = stripLiterals(commentsRemoved)
    .replace(/^\s*(?:global\s+)?using\s+.*$/gm, '')
    .replace(/^\s*namespace\s+.*$/gm, '');

  for (const [simpleName, frameworkType] of frameworkTypes) {
    const parents = namespaceParentsByLeaf.get(simpleName);
    if (!parents || aliases.has(simpleName)) continue;
    const collisionVisible = [...parents].some(parent => currentNamespace === parent || currentNamespace.startsWith(`${parent}.`));
    if (!collisionVisible) continue;
    const unqualified = new RegExp(`(^|[^A-Za-z0-9_:.])${simpleName}\\s*\\.`, 'm');
    assert.doesNotMatch(
      body,
      unqualified,
      `${path.relative(root, file)} uses unqualified ${simpleName} while a visible project namespace has that name. Use global::${frameworkType} or a deliberate alias.`
    );
  }
}

const program = fs.readFileSync(path.join(web, 'Program.cs'), 'utf8');
assert.match(program, /using PublisherStudio\.HostedServices\.Streaming;/);

for (const relative of [
  ['Services', 'Streaming', 'Chat', 'PlatformChatService.cs'],
  ['Services', 'Streaming', 'Lan', 'RtspLanServer.cs']
]) {
  const file = path.join(web, ...relative);
  const text = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(text, /(^|[^A-Za-z0-9_:.])Encoding\s*\./m);
  assert.match(text, /global::System\.Text\.Encoding\./);
}

console.log('PublisherStudio C# composition-root and namespace safety checks passed.');
