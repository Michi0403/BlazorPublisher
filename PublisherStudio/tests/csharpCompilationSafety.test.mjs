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
// deliberate alias or an explicit global:: qualification.
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
const appComposition = fs.readFileSync(path.join(web, 'PublisherStudioServiceCollectionExtensions.cs'), 'utf8');
assert.match(appComposition, /using PublisherStudio\.HostedServices\.Streaming;/);

for (const relative of [
  ['Services', 'Streaming', 'Chat', 'PlatformChatService.cs'],
  ['Services', 'Streaming', 'Lan', 'RtspLanServer.cs']
]) {
  const file = path.join(web, ...relative);
  const text = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(text, /(^|[^A-Za-z0-9_:.])Encoding\s*\./m);
  assert.match(text, /^using\s+TextEncoding\s*=\s*global::System\.Text\.Encoding\s*;/m);
  assert.match(text, /\bTextEncoding\s*\./);
}

// `global::` is legal in a normal C# expression, but when it begins an
// interpolation hole the first colon is parsed as the interpolation format
// separator. `$"{global::Type.Member}"` therefore resolves an identifier named
// `global` and produces CS0103. Require an alias/local value or parentheses.
for (const file of csharpFiles) {
  const text = stripComments(fs.readFileSync(file, 'utf8'));
  assert.doesNotMatch(
    text,
    /\{\s*global::/,
    `${path.relative(root, file)} starts an interpolation/expression hole with global::. Use a file-level alias, a local value, or parenthesize the global:: expression.`
  );
}

const rtsp = fs.readFileSync(path.join(web, 'Services', 'Streaming', 'Lan', 'RtspLanServer.cs'), 'utf8');
assert.match(rtsp, /var\s+contentLength\s*=\s*TextEncoding\.ASCII\.GetByteCount\(sdp\)\s*;/);
assert.match(rtsp, /\$"Content-Length:\s*\{contentLength\}"/);

// Razor control-flow bodies already are C# code blocks. Starting another `@{`
// inside one produces RZ1010. Keep the streaming-layer locals as normal C#
// declarations at the beginning of the existing `@if` body.
const inspectorPanel = fs.readFileSync(path.join(web, 'Components', 'Editor', 'InspectorPanel.razor'), 'utf8');
assert.match(
  inspectorPanel,
  /@if \(liveSource\.IsVisual\)\s*\{\s*var authoredLiveLayers = LiveEffectLayers\(liveSource\);\s*var selectedLiveLayer = SelectedLiveEffectLayer\(liveSource\);\s*var selectedLiveFilter = SelectedLiveEffectFilter\(liveSource\);/
);
assert.doesNotMatch(
  inspectorPanel,
  /@\{\s*var authoredLiveLayers = LiveEffectLayers\(liveSource\);/,
  'Do not nest an explicit Razor code block inside the existing liveSource.IsVisual control-flow body.'
);


// CS0136: NormalizeTemporalSelection already declares `end` later in the same
// local-variable declaration space. The provisional value must use a distinct
// name even though it appears in a nested if block.
const mediaTimelineEditService = fs.readFileSync(
  path.join(web, 'Services', 'MediaStudio', 'UseCases', 'MediaTimelineEditService.cs'),
  'utf8'
);
assert.match(
  mediaTimelineEditService,
  /if \(!segment\.TemporalSelectionCommitted\)\s*\{\s*var candidateEnd = double\.IsFinite\(segment\.TemporalSelectionEndSeconds\)/
);
assert.doesNotMatch(
  mediaTimelineEditService,
  /if \(!segment\.TemporalSelectionCommitted\)\s*\{\s*var end =/,
  'Do not redeclare end in NormalizeTemporalSelection; the method declares end later in the enclosing local-variable declaration space.'
);

// Every qualified PublicationLiveSourceKind reference must resolve to an actual
// enum member. This catches stale preset names such as PlatformChat before C#
// compilation reaches CS0117. Platform chat itself is modeled by the shared
// PublicationComponentKind.Chat control, not as a media-capture source.
const streamingModels = fs.readFileSync(path.join(web, 'Domain', 'PublicationStreamingModels.cs'), 'utf8');
const sourceKindBody = streamingModels.match(/public enum PublicationLiveSourceKind\s*\{([\s\S]*?)\}/)?.[1] ?? '';
const sourceKindMembers = new Set(
  sourceKindBody
    .split(',')
    .map(value => value.replace(/=.*/, '').trim())
    .filter(Boolean)
);
const razorFiles = [];
(function walkRazor(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === 'bin' || entry.name === 'obj') continue;
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walkRazor(full);
    else if (entry.isFile() && entry.name.endsWith('.razor')) razorFiles.push(full);
  }
})(web);
for (const file of [...csharpFiles, ...razorFiles]) {
  const text = stripLiterals(stripComments(fs.readFileSync(file, 'utf8')));
  for (const match of text.matchAll(/\bPublicationLiveSourceKind\.([A-Za-z_][A-Za-z0-9_]*)/g))
    assert.ok(
      sourceKindMembers.has(match[1]),
      `${path.relative(root, file)} references missing PublicationLiveSourceKind member ${match[1]}.`
    );
}

console.log('PublisherStudio C# composition-root, enum-reference, namespace, interpolation, Razor control-flow and local-scope safety checks passed.');

// Source-package closure: every project reference and every composition-root-only
// use-case that is required by a registration must be present in the delivered tree.
const projectFiles = [];
(function walkProjects(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (['bin', 'obj', 'node_modules', '.git', 'artifacts'].includes(entry.name)) continue;
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walkProjects(full);
    else if (entry.isFile() && entry.name.endsWith('.csproj')) projectFiles.push(full);
  }
})(root);
assert.ok(projectFiles.length >= 2, 'The source package must include the web and installer projects.');
for (const projectFile of projectFiles) {
  const xml = fs.readFileSync(projectFile, 'utf8');
  for (const match of xml.matchAll(/<ProjectReference\s+Include="([^"]+)"/g)) {
    const target = path.resolve(path.dirname(projectFile), match[1].replaceAll('\\', path.sep));
    assert.ok(fs.existsSync(target), `Missing ProjectReference ${match[1]} from ${path.relative(root, projectFile)}.`);
  }
}

const streamingRuntimePath = path.join(web, 'Services', 'Streaming', 'UseCases', 'Runtime', 'StreamingRuntimeUseCases.cs');
assert.ok(fs.existsSync(streamingRuntimePath), 'StreamingRuntimeUseCases.cs must be included in every source package.');
const streamingRuntime = fs.readFileSync(streamingRuntimePath, 'utf8');
assert.match(streamingRuntime, /namespace PublisherStudio\.Services\.Streaming\.UseCases\.Runtime;/);
assert.match(streamingRuntime, /public sealed class StreamingRuntimeUseCases/);
const streamingComposition = fs.readFileSync(path.join(web, 'StreamingServiceCollectionExtensions.cs'), 'utf8');
assert.match(streamingComposition, /AddSingleton<StreamingRuntimeUseCases>/);
assert.ok(globalImports.has('PublisherStudio.Services.Streaming.UseCases.Runtime'), 'The runtime use-case namespace must remain globally imported.');

const wireProjectPath = path.join(root, 'src', 'LocalGPT.WireProtocolVersion');
assert.equal(fs.existsSync(wireProjectPath), false, 'PublisherStudio must not carry a second protocol source project.');
const webProjectText = fs.readFileSync(path.join(web, 'PublisherStudio.Web.csproj'), 'utf8');
assert.match(webProjectText, /<PackageReference Include="LocalGPT\.WireProtocolVersion" Version="\$\(LocalGptWireProtocolVersion\)" \/>/);
assert.doesNotMatch(webProjectText, /ProjectReference[^>]*LocalGPT\.WireProtocolVersion/);
assert.match(fs.readFileSync(path.join(root, 'build', 'Ensure-WireProtocolPackage.ps1'), 'utf8'), /lib\/net10\.0\/LocalGPT\.WireProtocolVersion\.dll/);
console.log('PublisherStudio source-package project closure and StreamingRuntimeUseCases inventory checks passed.');
