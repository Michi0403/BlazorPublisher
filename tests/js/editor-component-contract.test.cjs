"use strict";
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const base = path.join(root, 'src/PublisherStudio.Web/Components');
function collectRazorFiles(folder) {
  return fs.readdirSync(folder, {withFileTypes:true}).flatMap(e => {
    const p = path.join(folder, e.name);
    return e.isDirectory() ? collectRazorFiles(p) : e.isFile() && e.name.endsWith('.razor') ? [p] : [];
  });
}
const files = collectRazorFiles(base);
const components = new Map(files.map(file => [path.basename(file, '.razor'), file]));
assert.equal(components.size, files.length, 'component names should be unique');
function declaredParameters(file) {
  const name = path.basename(file, '.razor');
  const directory = path.dirname(file);
  const partials = fs.readdirSync(directory).filter(n => n.startsWith(name + '.') && n.endsWith('.razor.cs'));
  const text = fs.readFileSync(file, 'utf8') + partials.map(n => '\n' + fs.readFileSync(path.join(directory, n), 'utf8')).join('');
  return new Set([...text.matchAll(/\[\s*Parameter(?:\s*,[^\]]*)?\]\s+public\s+[^\r\n{;=]+?\s+([A-Za-z_]\w*)\s*\{/gs)].map(m => m[1].toLowerCase()));
}
function unknownBindings(tag, declared) {
  return [...tag.matchAll(/(?<![@\w-])([A-Za-z_]\w*)\s*=\s*(?:"[^"]*"|'[^']*')/g)]
    .map(m => m[1]).filter(name => !declared.has(name.toLowerCase()));
}
// Independent negative control: this exact invalid binding crashed the released product.
const inspectorParameters = declaredParameters(components.get('InspectorPanel'));
assert.deepEqual(unknownBindings('OnInsertAiChat="InsertAiChatQuick"', inspectorParameters), ['OnInsertAiChat']);
const editor = fs.readFileSync(components.get('Editor'), 'utf8');
assert.match(editor, /<PublicationRibbon\b[\s\S]*?OnInsertAiChat="InsertAiChatQuick"[\s\S]*?\/>/);
const inspector = editor.match(/<InspectorPanel\b([\s\S]*?)\s*\/>/);
assert(inspector, 'InspectorPanel must remain mounted in Editor');
assert(!inspector[1].includes('OnInsertAiChat='), 'InspectorPanel cannot accept OnInsertAiChat');
assert.match(editor, /@onclick="InsertAiChatQuick"/);
let componentsInspected = 0, bindingsInspected = 0, editorInvocations = 0;
for (const source of files) {
  const markup = fs.readFileSync(source, 'utf8');
  const ownName = path.basename(source, '.razor');
  for (const name of new Set([...markup.matchAll(/<([A-Z][A-Za-z0-9_]*)\b/g)].map(m => m[1]))) {
    if (!components.has(name) || name === ownName) continue;
    // This pattern intentionally stops at the first UNQUOTED closing angle bracket.
    // A non-selfclosing tag must not swallow nested HTML and count it as parameters.
    const escape = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const pattern = new RegExp('<' + escape + '\\b((?:"[^"]*"|\x27[^\x27]*\x27|[^>"\x27])*?)\\s*/>', 'gs');
    const declared = declaredParameters(components.get(name));
    for (const match of markup.matchAll(pattern)) {
      componentsInspected++;
      if (ownName === 'Editor') editorInvocations++;
      const attrs = [...match[1].matchAll(/(?<![@\w-])([A-Za-z_]\w*)\s*=\s*(?:"[^"]*"|'[^']*')/g)];
      bindingsInspected += attrs.length;
      assert.deepEqual(unknownBindings(match[1], declared), [],
        `${path.relative(base, source)} passes undeclared parameter(s) to ${name}`);
    }
  }
}
assert(componentsInspected >= 70 && bindingsInspected >= 100 && editorInvocations >= 15,
  'contract coverage must not silently disappear');
const buildTargets = fs.readFileSync(path.join(root, 'Directory.Build.targets'), 'utf8');
const releaseScript = fs.readFileSync(path.join(root, 'Build-Release.ps1'), 'utf8');
assert.match(buildTargets, /<Target Name="AssertPublisherEditorComponentParameters"/);
assert.match(releaseScript, /build\/Assert-EditorComponentParameterContracts.ps1/);
console.log(`PASS: ${componentsInspected} application component contracts / ${bindingsInspected} bindings; editor first-render regression negative control`);
