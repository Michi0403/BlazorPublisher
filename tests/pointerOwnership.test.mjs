import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const tooltip = read('src/PublisherStudio.Web/wwwroot/js/tooltipRuntime.js');
const liveData = read('src/PublisherStudio.Web/wwwroot/js/liveDataInterop.js');
const exporter = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const surface = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
const print = read('src/PublisherStudio.Web/Components/Editor/PrintPublication.razor');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');

assert.match(tooltip, /pendingTarget === target/);
assert.match(tooltip, /target === activeTarget \|\| target === pendingTarget/);
assert.match(tooltip, /event\.composedPath\(\)/);
assert.match(tooltip, /popup\.showPopover\(\)/);
assert.match(tooltip, /popup\.hidePopover\(\)/);
assert.match(css, /\.publisher-help-tooltip\s*\{[\s\S]*?inset:\s*auto;[\s\S]*?margin:\s*0;/);

assert.match(liveData, /document\.addEventListener\("pointerover", clearVisualsOutside, true\)/);
assert.match(liveData, /instance\.hideTooltip\?\.\(\)/);
assert.match(liveData, /series\.clearHover\?\.\(\)/);
assert.match(liveData, /point\.clearHover\?\.\(\)/);
assert.match(liveData, /if \(!event\.relatedTarget\) clearAllVisualInteractions\(\)/);
assert.match(liveData, /function visualTooltip\(config\)/);
assert.match(liveData, /closest\?\.\("\.website-publication \[data-publication-element\]"\)/);
assert.match(liveData, /\{ enabled: true, container: exportedOwner \}/);
assert.ok((liveData.match(/tooltip: visualTooltip\(config\)/g) || []).length >= 3, 'Exported DevExtreme visual tooltips must use a transformed publication owner as their container.');

assert.equal((exporter.match(/const signalSourceIds = new Set/g) || []).length, 2, 'Presentation and site runtimes must classify Signal Arrow pointer sources.');
assert.match(exporter, /\['onclick', 'onhover'\]\.includes\(trigger\)/);
assert.match(exporter, /if \(nativeInteractive \|\| signalSource\) node\.classList\.add\('ps-pointer-owner'\)/);
assert.match(exporter, /\['shape', 'wordart', 'barcode'\]\.includes\(kind\)/);
assert.doesNotMatch(exporter, /\['shape', 'wordart', 'barcode', 'connector'\]/);
assert.equal((exporter.match(/\.ps-pointer-passive\{pointer-events:none!important\}/g) || []).length, 2, 'Presentation and site exports must share pointer-passive styling.');
assert.match(surface, /data-element-kind="connector"/);
assert.match(print, /data-element-kind="@element\.Kind\.ToString\(\)\.ToLowerInvariant\(\)"/);
assert.match(exporter, /function isDesignerComponentControlTarget\(target, element\)/);
assert.match(exporter, /\.dx-gallery-nav-button-prev/);
assert.match(exporter, /\.dx-gallery-nav-button-next/);
assert.match(exporter, /if \(!activeConnectorTool && isDesignerComponentControlTarget\(event\.target, element\)\)/);
assert.match(exporter, /The first click on[\s\S]*?selection-only/);

// Signal geometry and its source/target coordinate conversion remain in the dedicated runtime.
assert.match(exporter, /const pointTarget = \(connector, prefix\) =>/);
assert.ok(exporter.includes("const x = num(connector.dataset[`${prefix}X`]);"));
assert.ok(exporter.includes("const y = num(connector.dataset[`${prefix}Y`]);"));
assert.ok(exporter.includes("runner.setAttribute('transform', `translate(${point.x} ${point.y}) rotate(${angle})`);"));

console.log('tooltip top-layer, stable owner, export-local visual tooltip placement, embedded DevExtreme control ownership, chart hover cleanup, and Signal Arrow geometry contracts passed');
