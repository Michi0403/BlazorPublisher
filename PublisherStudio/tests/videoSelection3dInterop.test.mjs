import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const publicationModels = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationModels.cs');
const models = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationMediaModels.cs');
const panelModels = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationPanelModels.cs');
const timeline = read('src', 'PublisherStudio.Web', 'Services', 'MediaStudio', 'UseCases', 'MediaTimelineEditService.cs');
const studio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const view = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'VideoMediaView.razor');
const interop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');
const runtime = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'videoEffectRuntime.js');
const interchange = read('src', 'PublisherStudio.Web', 'Services', 'VideoStudio', 'Export', 'VideoLayerInterchangeService.cs');
const editor = read('src', 'PublisherStudio.Web', 'Components', 'Pages', 'Editor.razor');
const editorState = read('src', 'PublisherStudio.Web', 'Services', 'EditorStateService.cs');
const publicationFiles = read('src', 'PublisherStudio.Web', 'Services', 'PublicationFileService.cs');
const panelDocuments = read('src', 'PublisherStudio.Web', 'Services', 'Panels', 'PanelDocumentService.cs');
const ribbon = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PublicationRibbon.razor');
const inspector = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'InspectorPanel.razor');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

assert.match(models, /enum VideoEffectLayerKind \{ BaseVideo, Selection2D, Blob3D \}/);
assert.match(publicationModels, /enum PublicationHtmlExportSupport \{ Native, CanvasRuntime, RenderBeforeExport \}/);
assert.match(models, /VideoFrameRegion MorphRegion/);
assert.match(models, /bool AnimateMorph/);
assert.match(models, /string OpenScadScript/);
assert.match(timeline, /HtmlExportSupport = layer\.HtmlExportSupport/);
assert.match(timeline, /MorphRegion = new VideoFrameRegion/);

assert.match(studio, /Create\/reuse one effect layer for every committed range/);
assert.match(studio, /CreateOrReuseSelectionLayer\(forceNew: false, announce: false\)/);
assert.match(studio, /VideoEffectLayerKind\.Selection2D/);
assert.match(studio, /VideoEffectLayerKind\.Blob3D/);
assert.match(studio, /Draw morph target/);
assert.match(studio, /VideoFramePointSelected/);
assert.match(studio, /VideoFramePointCommitted[\s\S]*CommitFrameRegionToSelectedLayer\(\)[\s\S]*CommitSelectedSegment\(refreshVideoEffects: true\)/);
assert.match(studio, /RemoveSelectedFramePoint/);
assert.match(studio, /Delete selected region point/);
assert.match(studio, /Render before HTML export/);
assert.match(studio, /Insert selected 3D blob into Mainframe/);

assert.match(interop, /invokeMethodAsync\('VideoFramePointSelected'/);
assert.match(interop, /classList\.add\('selected'/);
assert.match(runtime, /resamplePolygon/);
assert.match(runtime, /activeRegion/);
assert.match(runtime, /blob3d/);
assert.match(runtime, /requestAnimationFrame/);
assert.match(view, /morphRegion/);
assert.match(view, /htmlExportSupport/);

assert.match(interchange, /CreateDefaultBlobLayer/);
assert.match(interchange, /function morph_points/);
assert.match(interchange, /ResamplePolygon/);
assert.match(interchange, /minkowski\(\)/);
assert.match(interchange, /linear_extrude/);
assert.match(interchange, /polygon\(points=pts\)/);
assert.match(interchange, /HTML canvas · OpenSCAD interchange/);
assert.match(interchange, /requestAnimationFrame\(draw\)/);
assert.match(interchange, /Native OpenSCAD mesh rendering must be baked before export/);

assert.match(editor, /OnInsertVideoBlob="InsertDefaultVideoBlob"/);
assert.match(editor, /VideoLayerInterchange\.CreateDefaultBlobLayer/);
assert.match(editor, /data-publisher-openscad/);
assert.match(editor, /State\.AddHtmlEmbed\(item =>/);
assert.match(editorState, /AddHtmlEmbed\(Action<HtmlEmbedElement> configure/);
assert.match(panelModels, /PublicationHtmlExportSupport HtmlExportSupport/);
assert.match(panelModels, /string InterchangeFormat/);
assert.match(inspector, /HtmlSupportLabel\(htmlEmbed.HtmlExportSupport\)/);
assert.match(inspector, /Render before HTML export/);
assert.match(inspector, /InterchangeFormat/);
assert.match(publicationFiles, /html.HtmlExportNote \?\?=/);
assert.match(panelDocuments, /html.InterchangeFormat \?\?=/);
assert.match(ribbon, /Interactive 3D blob/);
assert.match(ribbon, /OnInsertVideoBlob/);
assert.match(css, /media-html-support\.render/);
assert.match(css, /media-frame-node\.selected/);

console.log('PublisherStudio temporal selection layers, persistent point editing, 3D blob interchange, Mainframe reuse, animation, and HTML compatibility contracts passed.');
