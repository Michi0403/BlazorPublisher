import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const model = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationModels.cs');
const pictureModel = read('src', 'PublisherStudio.Web', 'Domain', 'PictureStudioModels.cs');
const pictureService = read('src', 'PublisherStudio.Web', 'Services', 'PictureDocumentService.cs');
const spreadsheet = read('src', 'PublisherStudio.Web', 'Services', 'SpreadsheetDocumentService.cs');
const inspector = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'InspectorPanel.razor');
const page = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PageSurface.razor');
const print = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PrintPublication.razor');
const pictureEditor = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor');
const pictureCode = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor.cs');
const runtime = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'publisherInterop.js');
const pictureRuntime = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'pictureStudioInterop.js');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

for (const contract of [
  'SignalConnector', 'SignalArrow', 'SignalConnectorSettings', 'SignalConnectorTrigger',
  'SignalConnectorVisual', 'SignalGesture', 'SignalCompletionAction', 'MotionTargetSelector',
  'CompletionTargetSelector', 'NextConnectorId', 'ResizeWidthPercent', 'ResizeHeightPercent'
]) assert.ok(model.includes(contract), `${contract} is missing from the signal model.`);

assert.match(page, /CommitSignalConnector/);
assert.match(page, /ReconnectSignalConnector/);
assert.match(page, /data-source-kind/);
assert.match(page, /data-target-kind/);
assert.match(page, /data-signal-enabled/);
assert.match(print, /data-signal="@PublicationAnimationData\.Signal\(connector\)"/);
assert.match(print, /id="element-@connector\.Id"/);

assert.match(inspector, /Signal Arrow \/ Connector/);
assert.match(inspector, /Start inner selector/);
assert.match(inspector, /Travel morph \/ transform/);
assert.match(inspector, /Cell, chart point, button/);
assert.match(inspector, /No server or network request is needed/);
assert.match(inspector, /Preview signal now/);
assert.match(inspector, /Resize width \(%\)/);
assert.match(inspector, /Resize height \(%\)/);

assert.match(runtime, /function signalConnectorRuntime\(/);
assert.match(runtime, /signalConnectorRuntime\.toString\(\)/, 'single-file HTML must embed the runtime source');
assert.match(runtime, /autoStart:false,expose:true/, 'single-file HTML must initialize the offline runtime without double-starting page signals');
assert.match(runtime, /waitForTarget/, 'signal targets rendered after page load must be awaited');
assert.match(runtime, /circular signal chain/, 'signal chaining must guard against cycles');
assert.match(runtime, /window\.PublisherStudioSignals = api/);
assert.match(runtime, /publisherSignalSynthetic/);
assert.match(runtime, /MutationObserver/);
assert.match(runtime, /const api = \{ run, stop, reset, startPage, dispose/);
assert.match(runtime, /page\.__publisherSignalRuntime\?\.reset/);
assert.match(runtime, /resizeWidthPercent/);
assert.match(runtime, /resizeHeightPercent/);
assert.doesNotMatch(runtime.slice(runtime.indexOf('function signalConnectorRuntime'), runtime.indexOf('function websitePresentationRuntime')), /publicationReducedMotion/, 'embedded signal runtime must be self-contained');
assert.match(runtime, /options\.finiteLoops !== true/);
assert.match(runtime, /videoSignals\?\.startPage\(page\)/);
assert.match(runtime, /videoSignals\?\.stop\(\)/);
assert.match(runtime, /signalChainDuration/);
assert.match(runtime, /publisher:page-enter/);
assert.match(runtime, /setVisibility/);
assert.match(runtime, /animateOpacity/);
assert.match(runtime, /\[data-content-viewport\]/);

assert.match(spreadsheet, /data-cell=\\"/);
assert.match(spreadsheet, /publisher-sheet-cell/);
assert.match(spreadsheet, /ColumnName\(column\)/);
assert.match(inspector, /\[data-cell='B4'\]/);

assert.match(pictureModel, /PictureShapeKind \{[^}]*Path/s);
assert.match(pictureModel, /PictureDrawTool \{[^}]*Path/s);
assert.match(pictureModel, /PathClosed/);
assert.match(pictureModel, /PathSmooth/);
assert.match(pictureService, /document\.FormatVersion = "1\.2"/);
assert.match(pictureEditor, /Download SVG/);
assert.match(pictureEditor, /Add point/);
assert.match(pictureEditor, /Reverse/);
assert.match(pictureCode, /PicturePathCommitted/);
assert.match(pictureCode, /ChangeShapePathPointX/);
assert.match(pictureRuntime, /const shapeKinds = \[[^\]]*"path"/);
assert.match(pictureRuntime, /function svgShapeLayer/);
assert.match(pictureRuntime, /function svgPathData/);
assert.match(pictureRuntime, /data-picture-layer-kind/);
assert.match(pictureRuntime, /downloadPictureStudioSvg/);
assert.match(css, /publisher-signal-runner/);
assert.match(css, /picture-path-point-row/);

console.log('offline signal connector reset/resize, spreadsheet targeting, SVG output, and path-tool contract tests passed');
