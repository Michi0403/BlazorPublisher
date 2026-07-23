import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const componentRuntime = read('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js');
const componentModel = read('src/PublisherStudio.Web/Domain/PublicationComponentModels.cs');
const componentService = read('src/PublisherStudio.Web/Services/PublicationComponentService.cs');
const componentEditor = read('src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor');
const dataModel = read('src/PublisherStudio.Web/Domain/PublicationDataModels.cs');
const dataService = read('src/PublisherStudio.Web/Services/PublicationDataService.cs');
const dataEditor = read('src/PublisherStudio.Web/Components/Editor/DataVisualEditor.razor');
const dataView = read('src/PublisherStudio.Web/Components/Editor/DataVisualView.razor');
const visualRuntime = read('src/PublisherStudio.Web/wwwroot/js/liveDataInterop.js');
const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const surface = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const publicationModel = read('src/PublisherStudio.Web/Domain/PublicationModels.cs');
const editorState = read('src/PublisherStudio.Web/Services/EditorStateService.cs');

// High-DPI editor zoom uses Chromium CSS zoom for layout/rasterization and keeps a transform fallback.
assert.match(surface, /--publisher-editor-zoom:\{Inv\(State\.Document\.Zoom\)\}/);
assert.match(interop, /CSS\?\.supports\?\.\("zoom", "1"\)/);
assert.match(interop, /content\.style\.zoom = String\(zoom\)/);
assert.match(css, /@supports \(zoom: 1\)[\s\S]*\.publication-element-content[\s\S]*zoom: var\(--publisher-editor-zoom, 1\)/);
assert.match(css, /\.publication-content-source \{ position:relative;box-sizing:border-box;transform-origin:0 0; \}/);
assert.match(css, /\.content-pan-target \.publication-content-source \{ will-change:transform; \}/);

// Provider maps are opt-in: no keyless Google default and no runtime request before provider+key exist.
assert.match(componentModel, /MapProvider \{ get; set; \} = string\.Empty/);
assert.match(componentModel, /MapId \{ get; set; \}/);
assert.doesNotMatch(componentService, /MapProvider = string\.IsNullOrWhiteSpace\(item\.MapProvider\) \? "google"/);
assert.match(componentRuntime, /hasMapProviderConfiguration/);
assert.match(componentRuntime, /renderMapConfigurationPlaceholder/);
assert.match(componentRuntime, /No external map request was made/);
assert.match(componentRuntime, /String\(config\.kind \|\| ""\) === "Map" && !hasMapProviderConfiguration\(config\)/);
assert.match(componentEditor, /Select a provider…/);
assert.match(componentEditor, /Use bundled keyless Vector Map instead/);
assert.match(componentEditor, /No provider preview request is made before both are present/);
assert.match(componentEditor, /Google Map ID \(optional\)/);
assert.match(componentRuntime, /providerConfig: googleProvider \? \{ mapId, useAdvancedMarkers: !!mapId \}/);

// Map viewport persistence waits until a real user gesture finishes, preventing slider rerender jumps.
assert.match(componentRuntime, /__psMapUserGesture/);
assert.match(componentRuntime, /__psMapGestureActive/);
assert.match(componentRuntime, /commitDesignerMapViewport\(element, config, 360\)/);
assert.match(componentRuntime, /setTimeout\(\(\) => \{[\s\S]*finish\(\);[\s\S]*\}, 520\)/);
assert.match(componentRuntime, /onReady\(\) \{ element\.__psMapReady = true; \}/);
assert.match(componentRuntime, /onDrawn\(\) \{ element\.__psMapReady = true; \}/);
assert.doesNotMatch(componentRuntime, /\}, 180\);/, 'the old mid-drag 180ms persistence timer must be gone');
assert.match(componentService, /PublicationComponentKind\.VectorMap \? 256d : 20d/);
assert.match(editorState, /PublicationComponentKind\.VectorMap \? 256d : 20d/);

// Real-data chart mappings now persist argument semantics, aggregation, and sorting.
for (const name of ['DataVisualArgumentMode', 'DataVisualAggregationMode', 'DataVisualSortMode']) assert.match(dataModel, new RegExp(`enum ${name}`));
for (const name of ['ArgumentMode', 'AggregationMode', 'SortMode']) assert.match(dataModel, new RegExp(`${name} \\{ get; set; \\}`));
assert.match(dataService, /argumentMode = item\.ArgumentMode\.ToString\(\)/);
assert.match(dataService, /aggregationMode = item\.AggregationMode\.ToString\(\)/);
assert.match(dataService, /sortMode = item\.SortMode\.ToString\(\)/);
assert.match(dataEditor, /Auto-map fields/);
assert.match(dataEditor, /Repeated categories/);
assert.match(dataEditor, /Mapping check/);
assert.match(dataEditor, /Bubble charts work best with three numeric roles/);
assert.match(dataEditor, /Financial charts require four numeric columns/);
assert.match(dataEditor, /No numeric or Boolean measure was detected/);
assert.match(visualRuntime, /function argumentValue\(/);
assert.match(visualRuntime, /function rangeScale\(/);
assert.match(visualRuntime, /function aggregateVisualPoints\(/);
assert.match(visualRuntime, /function sortVisualPoints\(/);
assert.match(visualRuntime, /argumentType: "datetime"/);
assert.match(visualRuntime, /case "average"/);
assert.match(visualRuntime, /case "count"/);
assert.match(dataView, /private bool UsesClientVisualization => true;/);
assert.match(publicationModel, /FormatVersion \{ get; set; \} = "1\.46"/);

// Creative studios fill the viewport with the same small shadow gap on every side.
assert.match(css, /--publisher-studio-shadow-gap: clamp\(8px, 1\.1vmin, 18px\)/);
assert.match(css, /\.modal-backdrop,[\s\S]*\.streaming-studio-backdrop,[\s\S]*\.spreadsheet-data-object-backdrop[\s\S]*padding: var\(--publisher-studio-shadow-gap\) !important/);
assert.match(css, /\.streaming-studio-window,[\s\S]*\.component-editor-dialog \{[\s\S]*width: 100% !important;[\s\S]*height: 100% !important;[\s\S]*max-width: none !important;/);

console.log('high-DPI zoom, provider-safe map, stable map viewport, real-data chart mapping, and full-screen studio contracts passed');
