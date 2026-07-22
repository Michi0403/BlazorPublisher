import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const studio = read('src/PublisherStudio.Web/Components/Editor/StreamingStudio.razor');
const surface = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const tooltips = read('src/PublisherStudio.Web/wwwroot/js/tooltipRuntime.js');
const liveData = read('src/PublisherStudio.Web/wwwroot/js/liveDataInterop.js');
const app = read('src/PublisherStudio.Web/Components/App.razor');
const liveSource = read('src/PublisherStudio.Web/Components/Editor/LiveSourceView.razor');
const streamingInterop = read('src/PublisherStudio.Web/wwwroot/js/streamingInterop.js');

for (const contract of [
  '<DxRibbon', '<DxContextMenu', 'Provider profiles', 'Publication outputs',
  'Recording plan', 'Company / LAN streaming', 'Reusable device profiles',
  'Advanced streaming options', 'WorkflowSteps', 'data-help='
]) assert.ok(studio.includes(contract), `Streaming Studio UI contract is missing: ${contract}`);

assert.match(surface, /OpenElementContextMenu/);
assert.match(surface, /ElementDesignerHelp/);
assert.match(surface, /data-help="Double-click to select this connector/);
assert.match(interop, /designerInteractionOwner/);
assert.match(interop, /addEventListener\('dblclick', handlers\.stageDoubleClick, true\)/);
assert.match(interop, /addEventListener\('contextmenu', handlers\.stageContextMenu, true\)/);
assert.match(interop, /stopImmediatePropagation\(\)/);
assert.match(interop, /number\(event\.pageX\)/);
assert.match(interop, /number\(event\.pageY\)/);
assert.match(surface, /PageX = pageX/);
assert.match(surface, /PageY = pageY/);
assert.match(surface, /Activate live source/);
assert.match(surface, /PublicationLiveSourceKind\.BrowserTab/);
assert.match(liveSource, /live-source-activate-/);
assert.match(streamingInterop, /function activateSource/);

assert.match(tooltips, /MutationObserver/);
assert.match(tooltips, /\[role="menuitem"\]/);
assert.match(tooltips, /publisherTooltip/);
assert.match(tooltips, /PublisherStudioTooltips/);
assert.match(tooltips, /function overlayZIndex/);
assert.match(tooltips, /dx-overlay-wrapper/);
assert.match(tooltips, /pendingTarget/);
assert.match(tooltips, /composedPath/);
assert.match(tooltips, /showPopover/);
assert.match(tooltips, /:popover-open/);
assert.match(tooltips, /next === current/);
assert.match(tooltips, /document\.addEventListener\('contextmenu', \(\) => hide\(true\)/);
assert.match(liveData, /function clearVisualInteraction/);
assert.match(liveData, /hideTooltip/);
assert.match(liveData, /clearHover/);
assert.match(liveData, /function bindPointerOwnership/);
assert.match(interop, /signalSourceIds/);
assert.match(interop, /ps-pointer-passive/);
assert.match(interop, /ps-pointer-owner/);
assert.match(surface, /data-element-kind=/);
assert.match(app, /js\/tooltipRuntime\.js/);

console.log('PublishingSuite interface, canvas activation/context-menu, and application tooltip contracts passed');
