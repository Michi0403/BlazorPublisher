import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const runtime = read('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js');
const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const surface = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
const view = read('src/PublisherStudio.Web/Components/Editor/DevExtremeComponentView.razor');
const editor = read('src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor');
const ribbon = read('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor');
const service = read('src/PublisherStudio.Web/Services/PublicationComponentService.cs');
const state = read('src/PublisherStudio.Web/Services/EditorStateService.cs');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');

// The mode is explicit in the component configuration and therefore cannot remain
// stale when selection or the global pointer mode changes.
assert.match(service, /designerInteractionMode = "content"/);
assert.match(service, /designerInteractionMode = string\.Equals\(designerInteractionMode, "object"/);
assert.match(view, /DesignerInteractionMode/);
assert.match(view, /data-ps-designer-interaction/);
assert.match(surface, /DesignerInteractionMode="@\(State\.ContentPanMode && State\.SelectedElementId == component\.Id \? "content" : "object"\)"/);
assert.match(editor, /ElementIdOverride="ps-component-vector-designer"[\s\S]*DesignerInteractionMode="content"/);

// Both provider maps and vector maps are shielded in object mode. Vector-map
// gestures and controls are enabled only in the explicit content mode.
assert.match(runtime, /const isMapKind = config => \["map", "vectormap"\]/);
assert.match(runtime, /const designerMapContentEnabled/);
assert.match(runtime, /panningEnabled: mapContentEnabled, zoomingEnabled: mapContentEnabled/);
assert.match(runtime, /controlBar: \{ enabled: config\.mapControls !== false && mapContentEnabled \}/);
assert.match(runtime, /if \(!config\?\.designerMode \|\| !isMapKind\(config\)\) return;/);
assert.match(runtime, /ps-component-designer-object-mode/);
assert.match(runtime, /ps-component-designer-content-mode/);
assert.match(runtime, /publisherstudio:map-viewport-changed/);
assert.match(runtime, /onCenterChanged\(event\)/);
assert.match(runtime, /onZoomFactorChanged\(event\)/);

// The canvas yields ownership only to the selected map in content mode. It does
// not start a competing object drag and it does not steal the wheel or dblclick.
assert.match(interop, /function componentMapContentInteractionActive/);
assert.match(interop, /selectedElementIdSet\(state\)\.has\(id\)/);
assert.match(interop, /state\.config\.contentPanMode = false/);
assert.match(interop, /classList\.toggle\('content-pan-target', contentPanTarget\)/);
assert.match(interop, /psDesignerInteraction/);
assert.match(interop, /if \(componentMapContentInteractionActive\(state, componentOwner\)\)[\s\S]*?return;/);
assert.match(interop, /if \(!owner \|\| componentMapContentInteractionActive\(state, owner\)\) return;/);
assert.match(interop, /if \(componentMapContentInteractionActive\(state, mapOwner\)\) return;/);
assert.match(interop, /CommitMapViewport/);
assert.match(interop, /stage\.addEventListener\('publisherstudio:map-viewport-changed'/);
assert.match(interop, /stage\.removeEventListener\('publisherstudio:map-viewport-changed'/);

// Selection changes terminate content mode, so an unselected or newly selected
// map cannot retain the previous object's native pan ownership.
assert.ok((state.match(/previousPrimary != SelectedElementId/g) || []).length >= 2);
assert.match(state, /if \(ContentPanMode\) ContentPanMode = false;/);
assert.match(state, /public void CommitMapViewport/);
assert.match(state, /SelectedElementId != id \|\| !ContentPanMode/);
assert.match(state, /component\.MapAutoAdjust = false/);

// The current mouse owner is visible and directly switchable in the editor.
assert.match(surface, /Pan \/ zoom map content/);
assert.match(surface, /Move map object/);
assert.match(surface, /ToggleMapMouseMode/);
assert.match(surface, /content-pan-target/);
assert.match(ribbon, /Mouse: pan \/ zoom map/);
assert.match(ribbon, /Mouse: move map object/);

// Content-mode pointer styling is scoped to the active selected object. The old
// global selector would disable interaction for every component on the page.
assert.match(css, /\.content-pan-mode \.pub-element\.content-pan-target/);
assert.doesNotMatch(css, /\.content-pan-mode \[data-content-fit-source\]/);
assert.match(css, /\.ps-component-designer-map-shield/);
assert.match(css, /not\(\.content-pan-target\)[\s\S]*ps-component-designer-content-mode[\s\S]*pointer-events:\s*none/);

console.log('explicit map object/content mouse modes, selected-owner gating, viewport persistence, and scoped pointer styling contracts passed');
