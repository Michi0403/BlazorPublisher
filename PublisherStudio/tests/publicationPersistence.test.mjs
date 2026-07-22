import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const files = read('src/PublisherStudio.Web/Services/PublicationFileService.cs');
const state = read('src/PublisherStudio.Web/Services/EditorStateService.cs');
const streamStore = read('src/PublisherStudio.Web/Services/PublicationStreamingSettingsStore.cs');
const program = read('src/PublisherStudio.Web/Program.cs');
const preview = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const timeline = read('src/PublisherStudio.Web/wwwroot/js/timelineInterop.js');
const components = read('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');

// Publication/editor settings travel with the publication and now mark it dirty.
assert.match(files, /SerializeToNode\(document, _options\)/);
assert.match(files, /root\.Remove\(streamingProperty\)/);
assert.match(files, /HasEmbeddedStreamingSettings/);
assert.match(state, /public void SetZoom[\s\S]*?Document\.Zoom = normalized;[\s\S]*?Notify\(\);/);
assert.match(state, /public void SetRulerUnit[\s\S]*?Document\.View\.RulerUnit = unit;[\s\S]*?Notify\(\);/);
assert.match(state, /public void SetViewOption[\s\S]*?update\(Document\.View\);[\s\S]*?Notify\(\);/);

// Stream routing/recording/LAN/hotkey configuration stays local and protected.
assert.match(program, /AddSingleton<PublicationStreamingSettingsStore>\(\)/);
assert.match(streamStore, /SpecialFolder\.LocalApplicationData/);
assert.match(streamStore, /CreateProtector\("PublisherStudio\.PublicationStreamingSettings\.v1"\)/);
assert.match(streamStore, /_protector\.Protect\(json\)/);
assert.match(streamStore, /_protector\.Unprotect\(protectedPayload\)/);
assert.match(state, /public void ApplyStreamingSettings[\s\S]*?PersistStreamingSettings\(\);[\s\S]*?Notify\(false\);/);
assert.doesNotMatch(state.match(/public void ApplyStreamingSettings[\s\S]*?\n    }/)?.[0] || '', /Capture\(\)/);
assert.match(state, /private void Restore[\s\S]*?var streaming = Document\.Streaming;[\s\S]*?Document\.Streaming = streaming;/);

// Every preview run captures the stable inline transform after clearing the old run.
assert.match(preview, /function baseTransform\(node\)[\s\S]*?node\?\.style\?\.transform/);
assert.match(preview, /clearPublicationPreview\(root\.id \|\| root\);/);
assert.match(preview, /baseTransforms: new Map\(\)/);
assert.match(preview, /state\.baseTransforms\?\.set\?\.\(node, baseTransform\(node\)\)/);
assert.match(preview, /runPublicationAnimation\(item\.node, item\.animation, start, state\.baseTransforms\)/);
assert.match(preview, /baseTransforms\?\.get\?\.\(member\) \?\? null/);
assert.match(timeline, /function baseTransform\(node\)[\s\S]*?node\?\.style\?\.transform/);

// A designer-only transparent shield prevents dxMap from panning while its frame is dragged.
assert.match(components, /function installDesignerMapShield/);
assert.match(components, /config\?\.designerMode/);
assert.match(components, /lower\(config\.kind\) !== "map"/);
assert.match(components, /stopImmediatePropagation\?\.\(\)/);
assert.match(components, /installDesignerMapShield\(element, config\)/);
assert.match(css, /\.ps-component-designer-map-shield/);
assert.match(css, /touch-action:none/);
assert.match(css, /cursor:move/);


function extractFunction(source, name) {
  const start = source.indexOf(`function ${name}`);
  assert.notEqual(start, -1, `${name} was not found.`);
  const bodyStart = source.indexOf('{', start);
  let depth = 0;
  for (let index = bodyStart; index < source.length; index++) {
    if (source[index] === '{') depth++;
    else if (source[index] === '}' && --depth === 0) return source.slice(start, index + 1);
  }
  throw new Error(`${name} has no closing brace.`);
}

// Runtime proof for the preview regression: a sampled animation matrix must not
// replace the publication's authored inline transform on the next Preview run.
{
  const baseTransform = Function('getComputedStyle', `${extractFunction(preview, 'baseTransform')}; return baseTransform;`)(
    () => ({ transform: 'matrix(2, 0, 0, 2, 80, 30)' })
  );
  assert.equal(baseTransform({ style: { transform: 'rotate(15deg)' } }), 'rotate(15deg)');
  assert.equal(baseTransform({ style: { transform: '' } }), 'matrix(2, 0, 0, 2, 80, 30)');
}

// Runtime proof that the map shield is designer-only and consumes provider gestures.
{
  const listeners = new Map();
  const children = [];
  const element = {
    querySelector() { return null; },
    append(child) { children.push(child); }
  };
  const documentMock = {
    createElement() {
      return {
        className: '',
        setAttribute() {},
        addEventListener(type, handler) { listeners.set(type, handler); }
      };
    }
  };
  const install = Function('document', 'lower', `${extractFunction(components, 'installDesignerMapShield')}; return installDesignerMapShield;`)(
    documentMock,
    value => String(value || '').toLowerCase()
  );
  install(element, { designerMode: true, kind: 'Map' });
  assert.equal(children.length, 1);
  assert.ok(listeners.has('pointerdown'));
  let prevented = false;
  let stopped = false;
  listeners.get('pointerdown')({
    preventDefault() { prevented = true; },
    stopImmediatePropagation() { stopped = true; }
  });
  assert.equal(prevented, true);
  assert.equal(stopped, true);

  children.length = 0;
  listeners.clear();
  install(element, { designerMode: false, kind: 'Map' });
  assert.equal(children.length, 0);
}

console.log('publication settings, preview restart, local stream settings, and designer map-drag contracts passed');
