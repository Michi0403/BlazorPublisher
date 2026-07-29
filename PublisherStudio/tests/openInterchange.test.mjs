import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const pictureModels = read('src', 'PublisherStudio.Web', 'Domain', 'PictureStudioModels.cs');
const publicationModels = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationModels.cs');
const interchangeModels = read('src', 'PublisherStudio.Web', 'Domain', 'InterchangeModels.cs');
const sanitizer = read('src', 'PublisherStudio.Web', 'Services', 'PictureStudio', 'Import', 'SvgInterchangeSanitizer.cs');
const openRaster = read('src', 'PublisherStudio.Web', 'Services', 'PictureStudio', 'Import', 'OpenRasterImportService.cs');
const openDocument = read('src', 'PublisherStudio.Web', 'Services', 'Publication', 'Import', 'OpenDocumentImportService.cs');
const pictureEditor = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor');
const pictureEditorCode = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor.cs');
const inspector = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'InspectorPanel.razor');
const wordArtView = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'WordArtView.razor');
const printPublication = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PrintPublication.razor');
const editor = read('src', 'PublisherStudio.Web', 'Components', 'Pages', 'Editor.razor');
const ribbon = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PublicationRibbon.razor');
const pictureInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'pictureStudioInterop.js');
const publisherInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'publisherInterop.js');
const program = read('src', 'PublisherStudio.Web', 'Program.cs');
const applicationComposition = read('src', 'PublisherStudio.Web', 'PublisherStudioServiceCollectionExtensions.cs');
const project = read('src', 'PublisherStudio.Web', 'PublisherStudio.Web.csproj');
const packageJson = JSON.parse(read('src', 'PublisherStudio.Web', 'package.json'));
const docs = read('docs', 'architecture', 'interchange-formats.md');
const adr = read('docs', 'decisions', 'ADR-007-open-interchange-adapters.md');
const agents = read('AGENTS.md');
const layeredSvgFixture = read('tests', 'fixtures', 'interchange', 'layered-inkscape.svg');
const unsafeSvgFixture = read('tests', 'fixtures', 'interchange', 'unsafe-online.svg');
const openRasterFixture = read('tests', 'fixtures', 'interchange', 'openraster-stack.xml');
const openDocumentFixture = read('tests', 'fixtures', 'interchange', 'opendocument-content.xml');


// Representative fixtures cover Inkscape-style layer groups, hidden paths, retained paint servers,
// unsafe online content, OpenRaster stack semantics, and flat OpenDocument inline assets.
assert.match(layeredSvgFixture, /inkscape:groupmode="layer"/);
assert.match(layeredSvgFixture, /<path id="main-path"/);
assert.match(layeredSvgFixture, /style="display:none"/);
assert.match(layeredSvgFixture, /clip-path="url\(#badgeClip\)"/);
assert.match(unsafeSvgFixture, /<script>/);
assert.match(unsafeSvgFixture, /https:\/\/example\.invalid/);
assert.match(unsafeSvgFixture, /relative-pixel\.png/);
assert.match(openRasterFixture, /<stack name="Background group"/);
assert.match(openRasterFixture, /visibility="hidden"/);
assert.match(openDocumentFixture, /<draw:page/);
assert.match(openDocumentFixture, /<office:binary-data>/);
assert.match(openDocumentFixture, /<draw:path/);

// Canonical native models remain authoritative; interchange adapters return explicit issues.
assert.match(pictureModels, /FormatVersion \{ get; set; \} = "1\.4"/);
assert.match(pictureModels, /JsonDerivedType\(typeof\(SvgPictureLayer\), "svg"\)/);
assert.match(pictureModels, /public string GroupPath/);
assert.match(pictureModels, /sealed class SvgPictureLayer/);
assert.match(interchangeModels, /enum InterchangeIssueSeverity \{ Information, Warning, Loss \}/);
assert.match(interchangeModels, /sealed class PictureImportResult/);
assert.match(interchangeModels, /sealed class PublicationImportResult/);
assert.match(publicationModels, /FormatVersion \{ get; set; \} = "1\.55"/);

// SVG is retained as vector markup and sanitized before it enters the canonical model.
assert.match(sanitizer, /DtdProcessing = DtdProcessing\.Prohibit/);
assert.match(sanitizer, /"script", "foreignObject", "iframe", "object", "embed", "audio", "video", "canvas"/);
assert.match(sanitizer, /local\.StartsWith\("on"/);
assert.match(sanitizer, /ContainsExternalCssReference/);
assert.match(sanitizer, /IsExternalReference/);
assert.match(pictureInterop, /export async function importPictureStudioSvg/);
assert.match(pictureInterop, /svgImportVisualSelector = "path,rect,circle,ellipse,line,polyline,polygon,text,image,use"/);
assert.match(pictureInterop, /getBBox/);
assert.match(pictureInterop, /getCTM/);
assert.match(pictureInterop, /rootMatrix/);
assert.match(pictureInterop, /hasUnsafeSvgCssReference/);
assert.match(pictureInterop, /standaloneSvgForElement/);
assert.match(pictureInterop, /groupPath: svgImportLayerPath\(element\)/);
assert.match(pictureInterop, /SVG_EXTERNAL_IMAGE_SKIPPED/);
assert.match(pictureInterop, /svgImportElementVisible/);
assert.match(pictureInterop, /revealSvgElementForMeasurement/);
assert.match(pictureInterop, /for \(const element of \[root, \.\.\.root\.querySelectorAll/);
assert.match(pictureInterop, /data-publisherstudio-picture/);
assert.match(pictureInterop, /formatVersion: "1\.4"/);

// OpenRaster uses only BCL ZIP/XML and preserves layer order, groups, opacity, visibility and locks.
assert.match(openRaster, /ZipArchive/);
assert.match(openRaster, /stack\.xml/);
assert.match(openRaster, /flattened\.Reverse\(\)/);
assert.match(openRaster, /GroupPath = groupPath/);
assert.match(openRaster, /layerOpacity/);
assert.match(openRaster, /layerVisible/);
assert.match(openRaster, /edit-locked/);
assert.match(openRaster, /svgSanitizer\.Sanitize/);
assert.match(openRaster, /ORA_LAYER_SOURCE_INVALID/);
assert.match(openRaster, /CopyToAsync/);
assert.doesNotMatch(openRaster, /CopyWithLimitAsync|64 MB decompression limit/, 'Local-first imports must not impose an application-defined media byte ceiling.');
assert.match(openRaster, /ORA_SVG_LAYER_INVALID/);
assert.match(openRaster, /private int ReadInt/);
assert.doesNotMatch(openRaster, /private static int ReadInt/);
assert.match(openRaster, /private double ReadDouble/);
assert.doesNotMatch(openRaster, /private static double ReadDouble/);
assert.match(openRaster, /private PictureBlendMode MapBlendMode/);
assert.doesNotMatch(openRaster, /private static PictureBlendMode MapBlendMode/);
assert.match(openRaster, /new GZipStream/);
assert.match(openRaster, /private int ReadBigEndianInt/);
assert.doesNotMatch(openRaster, /private static int ReadBigEndianInt/);
assert.doesNotMatch(openRaster, /var viewport =/);
assert.doesNotMatch(openRaster, /var size = ReadImageSize/);

// OpenDocument Drawing/Presentation imports map pages and common objects into the native page system.
assert.match(openDocument, /\.fodg" or "\.fodp/);
assert.match(openDocument, /content\.xml/);
assert.match(openDocument, /Descendants\(Draw \+ "page"\)/);
assert.match(openDocument, /new PublicationPage/);
assert.match(openDocument, /ImageFrameElement/);
assert.match(openDocument, /TextFrameElement/);
assert.match(openDocument, /ShapeElement/);
assert.match(openDocument, /SvgPictureLayer/);
assert.match(openDocument, /ODF_CUSTOM_SHAPE_APPROXIMATED/);
assert.match(openDocument, /chain\.Reverse\(\)/);
assert.match(openDocument, /NormalizePackagePath/);
assert.match(openDocument, /DtdProcessing = DtdProcessing\.Prohibit/);
assert.match(openDocument, /Office \+ "binary-data"/);
assert.match(openDocument, /ODF_GROUP_TRANSFORM_APPROXIMATED/);
assert.match(openDocument, /ResolveImageMime/);

// User-facing import entry points stay inside their owning studios/mainframe and use Services.
assert.match(pictureEditor, /Layered SVG \/ OpenRaster/);
assert.match(pictureEditor, /accept="\.svg,\.svgz,\.ora/);
assert.match(pictureEditorCode, /OpenRasterImportService OpenRasterImporter/);
assert.match(pictureEditorCode, /importPictureStudioSvg/);
assert.match(ribbon, /Import OpenDocument pages/);
assert.match(editor, /OpenDocumentImportService OpenDocumentImporter/);
assert.match(editor, /accept="\.odg,\.odp,\.fodg,\.fodp/);
assert.match(applicationComposition, /AddSingleton<OpenRasterImportService, OpenRasterImportService>/);
assert.match(applicationComposition, /AddSingleton<OpenDocumentImportService, OpenDocumentImportService>/);

// The path tool is node-based rather than an alias for a brush stroke.
assert.match(pictureInterop, /function addPathNode/);
assert.match(pictureInterop, /function finishPathDraft/);
assert.match(pictureEditorCode, /Double-click or press Enter/i);
assert.match(pictureInterop, /PicturePathCommitted/);
assert.match(pictureEditorCode, /PicturePathCommitted\(double\[\] coordinates, bool closed, bool smooth\)/);

// WordArt picture/video fills are canonical, editable and rendered through the same glyph mask.
assert.match(publicationModels, /enum WordArtFillKind \{ Solid, Gradient, Picture, Video \}/);
assert.match(publicationModels, /FillMediaDataUrl/);
assert.match(publicationModels, /FillMediaPosterDataUrl/);
assert.match(inspector, /Choose picture…/);
assert.match(inspector, /Choose video…/);
assert.match(inspector, /ImportWordArtPicture/);
assert.match(inspector, /SvgSanitizer\.Sanitize/);
assert.match(inspector, /ImportWordArtVideo/);
assert.match(wordArtView, /<mask id="@MediaMaskId"/);
assert.match(wordArtView, /<image class="wordart-media-fill/);
assert.match(wordArtView, /<video src="@VideoMediaSource"/);
assert.match(wordArtView, /UsesStaticMediaImage/);
assert.match(printPublication, /StaticMediaFill="true"/);
assert.match(publisherInterop, /function freezeMediaForRaster/);
assert.match(publisherInterop, /inlineLocalMediaSources/);

// Architecture and dependency rules are explicit and no package was added for interchange.
assert.match(agents, /Open specifications do not automatically permit adding an implementation package/);
assert.match(agents, /Adapters belong under the owning `Services\/<Area>\/Import`/);
assert.match(agents, /DTD processing must be prohibited/);
assert.match(docs, /SVG \/ SVGZ/);
assert.match(docs, /OpenRaster/);
assert.match(docs, /OpenDocument Drawing/);
assert.match(docs, /OpenDocument Presentation/);
assert.match(adr, /No new NuGet or JavaScript package/);
assert.deepEqual(Object.keys(packageJson.dependencies).sort(), [
  'devexpress-aspnetcore-spreadsheet',
  'devextreme-dist',
  'jquery'
]);
assert.doesNotMatch(project, /PackageReference[^>]+(?:Svg|OpenRaster|OpenDocument|Skia|Sharp)/i);

function extractFunction(source, name) {
  const start = source.indexOf(`function ${name}`);
  assert.notEqual(start, -1, `${name} was not found.`);
  const bodyStart = source.indexOf('{', start);
  let depth = 0;
  let quote = '';
  let escaped = false;
  for (let index = bodyStart; index < source.length; index++) {
    const char = source[index];
    if (quote) {
      if (escaped) escaped = false;
      else if (char === '\\') escaped = true;
      else if (char === quote) quote = '';
      continue;
    }
    if (char === '"' || char === "'" || char === '`') { quote = char; continue; }
    if (char === '{') depth++;
    else if (char === '}' && --depth === 0) return source.slice(start, index + 1);
  }
  throw new Error(`${name} has no closing brace.`);
}

// Runtime checks for the pure interchange helpers and path commit contract.
{
  const safeSvgReference = Function(`${extractFunction(pictureInterop, 'safeSvgReference')}; return safeSvgReference;`)();
  assert.equal(safeSvgReference('#gradient'), '#gradient');
  assert.equal(safeSvgReference('data:image/png;base64,AA=='), 'data:image/png;base64,AA==');
  assert.equal(safeSvgReference('data:image/svg+xml;base64,PHN2Zz4='), '');
  assert.equal(safeSvgReference('relative-image.png'), '');
  assert.equal(safeSvgReference('https://example.invalid/image.png'), '');
  assert.equal(safeSvgReference('//example.invalid/image.png'), '');
}

{
  const svgNumberValue = Function(`${extractFunction(pictureInterop, 'svgNumberValue')}; return svgNumberValue;`)();
  assert.ok(Math.abs(svgNumberValue('25.4mm') - 96) < 0.001);
  assert.equal(svgNumberValue('2in'), 192);
  assert.equal(svgNumberValue('50%', 320), 320);
}
{
  let callback = null;
  let renders = 0;
  const finishPathDraft = Function('safeInvoke', 'scheduleEditorRender', `${extractFunction(pictureInterop, 'finishPathDraft')}; return finishPathDraft;`)(
    (_editor, method, ...args) => { callback = { method, args }; },
    () => { renders++; }
  );
  const editorState = { pathDraft: { points: [{ x: 10, y: 20 }, { x: 30, y: 45 }, { x: 70, y: 80 }] } };
  finishPathDraft(editorState, true);
  assert.equal(editorState.pathDraft, null);
  assert.equal(callback.method, 'PicturePathCommitted');
  assert.deepEqual(callback.args, [[10, 20, 30, 45, 70, 80], true, false]);
  assert.equal(renders, 1);
}

console.log('open interchange, layered SVG/OpenRaster, node path tool, OpenDocument page import, and WordArt media-fill contracts passed');
