import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('render-mode guard accepts Razor page directives before rendermode', () => {
  const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
  assert.match(editor, /^@page "/m);
  assert.match(editor, /^@rendermode InteractiveServer$/m);
  const guard = read('build/Assert-InteractiveServerRenderModes.ps1');
  assert.match(guard, /\(\?m\)\^\\s\*\$escapedDirective\\s\*\$/);
  assert.match(guard, /Expected directive/);
  assert.doesNotMatch(guard, /\$first\s*=|\$first\s+-cne/);
});
