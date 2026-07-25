import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const walk = directory => fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
  const full = path.join(directory, entry.name);
  return entry.isDirectory() ? walk(full) : [full];
});

const panelStudio = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
const panelPreview = read('src/PublisherStudio.Web/Components/Editor/PanelElementPreview.razor');
const panelService = read('src/PublisherStudio.Web/Services/Panels/PanelDocumentService.cs');
const panelModels = read('src/PublisherStudio.Web/Domain/PublicationPanelModels.cs');
const publicationModels = read('src/PublisherStudio.Web/Domain/PublicationModels.cs');
const publicationFiles = read('src/PublisherStudio.Web/Services/PublicationFileService.cs');
const converterStudio = read('src/PublisherStudio.Web/Components/Editor/MediaConverterStudio.razor');
const conversionModels = read('src/PublisherStudio.Web/Domain/MediaConversionModels.cs');
const conversionService = read('src/PublisherStudio.Web/Services/MediaConversion/MediaConversionService.cs');
const conversionController = read('src/PublisherStudio.Web/Controllers/MediaConversionController.cs');
const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
const mediaStudio = read('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor');
const mediaInterop = read('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js');
const publisherInterop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const siteCss = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const program = read('src/PublisherStudio.Web/Program.cs');

// Panel / Div Studio is a real shared-component composer, not a JSON-only form.
assert.match(panelStudio, /<DxRibbon[\s\S]*<DxRibbonTab Text="Insert">/);
assert.match(panelStudio, /class="panel-component-tool[\s\S]*draggable="true"/);
assert.match(panelStudio, /<PanelView Document="Document" Item="_draft"/);
assert.match(panelStudio, /<PanelElementPreview Item="_dragPrototype"/);
assert.match(panelStudio, /@ondragover:preventDefault[\s\S]*@ondrop="DropDraggedElement"/);
assert.match(panelStudio, /bindPanelStudioDropSurface/);
assert.match(panelStudio, /CommitPanelElementBounds/);
assert.match(panelStudio, /data-resize="se"/);
assert.doesNotMatch(panelStudio, /TrackDrag\(DragEventArgs/, 'High-frequency drag preview movement must stay in browser JavaScript.');
assert.match(panelStudio, /Save\/update module/);
assert.match(panelStudio, /Save as new module/);
assert.match(panelStudio, /Files\.SerializeElement\(SelectedElement\)/);
assert.match(panelStudio, /Files\.DeserializeElement\(_advancedElementJson\)/);
assert.match(panelPreview, /case DataVisualElement visual/);
assert.match(panelPreview, /case DevExtremeComponentElement component/);
assert.match(panelPreview, /case LiveSourceElement live/);
assert.match(panelPreview, /case PanelElement panel/);
assert.match(panelService, /GetComponentTools\(PublicationDocument document\)/);
assert.match(panelService, /CreateComponentTool\(PublicationDocument document, string toolId\)/);
assert.match(panelService, /_components\.Create\(document, kind\)/);
assert.match(panelModels, /sealed class PublicationElementTemplate/);
assert.match(publicationModels, /List<PublicationElementTemplate> ComponentTemplates/);
assert.match(publicationFiles, /SerializeElement\(PublicationElement element\)/);
assert.match(publicationFiles, /DeserializeElement\(string json\)/);
assert.match(siteCss, /\.panel-studio-drag-ghost/);
assert.match(siteCss, /\.panel-element-preview/);
assert.match(siteCss, /\.panel-studio-canvas-shell\.arrange-preview \.panel-studio-canvas/);

// Media Converter Studio exposes the reusable service through a complete local UI.
assert.match(converterStudio, /<DxRibbon[\s\S]*<DxRibbonTab Text="Video">[\s\S]*<DxRibbonTab Text="Audio & filters">/);
assert.match(converterStudio, /<DxContextMenu/);
assert.match(converterStudio, /ShowJobContextMenu\(job, args\)/);
assert.match(converterStudio, /Picture Studio/);
assert.match(converterStudio, /Audio Studio/);
assert.match(converterStudio, /title="Close Media Converter Studio"/);
assert.match(converterStudio, /bindMediaConverterDrop/);
assert.match(converterStudio, /SourceStudioCommand/);
assert.match(converterStudio, /SelectedStudioCommand/);
assert.match(converterStudio, /PublisherStudio HTML/);
assert.match(converterStudio, /Advanced FFmpeg arguments/);
assert.match(converterStudio, /<label>Width<input type=\"number\"/);
assert.match(converterStudio, /<label>Height<input type=\"number\"/);
assert.match(converterStudio, /Save profile/);
assert.match(conversionModels, /enum MediaConversionScaleMode[\s\S]*Stretch/);
for (const property of ['StartSeconds', 'DurationSeconds', 'Width', 'Height', 'FrameRate', 'VideoCodec', 'Crf', 'VideoBitrateKbps', 'PixelFormat', 'AudioCodec', 'AudioBitrateKbps', 'AudioSampleRate', 'AudioChannels', 'VideoFilter', 'AudioFilter', 'AdvancedArguments', 'OutputExtension']) {
  assert.match(conversionModels, new RegExp(`\\b${property}\\b`), `Missing reusable media-conversion option ${property}.`);
}
assert.match(conversionService, /ProcessStartInfo[\s\S]*UseShellExecute = false/);
assert.match(conversionService, /startInfo\.ArgumentList\.Add/);
assert.match(conversionService, /ParseAdvancedArguments/);
assert.match(conversionService, /ForbiddenAdvancedOptions/);
assert.match(conversionService, /Preserve FFmpeg's[\s\S]*filter\/path escapes/);
assert.match(conversionService, /next == '\\\\' \|\| next is/);
assert.match(conversionService, /PublisherStudio HTML · balanced/);
assert.match(conversionService, /Browser compatibility · MP4/);
assert.match(conversionService, /Editing intermediate · ProRes/);
assert.match(conversionService, /Lossless archive · FFV1/);
assert.match(conversionController, /RequestFormLimits\(MultipartBodyLengthLimit = long\.MaxValue\)/);
assert.match(conversionController, /RequestSizeLimit\(long\.MaxValue\)/);
assert.match(publisherInterop, /bindMediaConverterDrop/);
assert.match(publisherInterop, /bindPanelStudioDropSurface/);
assert.match(publisherInterop, /ghost\.style\.left/);
assert.match(publisherInterop, /invokeMethodAsync\('CommitPanelElementBounds'/);
assert.match(publisherInterop, /window\.addEventListener\('pointermove', move/);
assert.match(publisherInterop, /ReceiveMediaDropError/);

// Mainframe, VideoStudio and converter hand-offs use the shared contract.
assert.match(editor, /InitialSource="_mediaConverterInitialSource"/);
assert.match(editor, /OpenMediaConverterFromStudio/);
assert.match(editor, /OpenConvertedInStudio/);
assert.match(editor, /Mainframe video export/);
assert.match(mediaStudio, /Send selection to converter/);
assert.match(mediaStudio, /SendSelectedMediaToConverter/);
assert.match(mediaStudio, /SuggestedOptions = IsVideo/);
assert.match(mediaStudio, /SuggestedPresetId = IsVideo \? "webm-vp9" : "audio-webm-opus"/);
assert.match(editor, /mime.StartsWith\("image\/"/);
assert.match(editor, /new AudioElement/);

// Visible selection geometry and drop insertion use the selected clip trim window.
assert.match(mediaStudio, /visibleStart = Math\.Max\(0, Math\.Min\(_duration, _trimStart\)\)/);
assert.match(mediaStudio, /\(VideoSelectionStart - visibleStart\) \/ visibleSpan/);
assert.match(mediaInterop, /const visibleSpan = Math\.max\(\.01, trimEnd - trimStart\)/);
assert.match(mediaInterop, /const raw = trimStart \+ ratio \* visibleSpan/);
assert.match(mediaInterop, /requestVideoDurationProbe/);
assert.match(mediaInterop, /video\.currentTime = Number\.MAX_SAFE_INTEGER/);
assert.match(mediaInterop, /const modeled = Math\.max\(\.01, Number\(overlay\?\.dataset\?\.duration\)/);

// Local-first imports do not impose an application-defined byte ceiling.
assert.match(program, /options\.Limits\.MaxRequestBodySize = null/);
assert.match(program, /options\.MultipartBodyLengthLimit = long\.MaxValue/);
const sourceFiles = walk(path.join(root, 'src', 'PublisherStudio.Web'))
  .filter(file => /\.(?:cs|razor)$/.test(file));
for (const file of sourceFiles) {
  const source = fs.readFileSync(file, 'utf8');
  for (const match of source.matchAll(/OpenReadStream\(([^)]*)\)/g)) {
    const argument = match[1].trim();
    // Parameterless IFormFile.OpenReadStream is controlled by unlimited request settings.
    if (!argument) continue;
    assert.equal(argument, 'long.MaxValue', `${path.relative(root, file)} still applies an application byte ceiling: ${match[0]}`);
  }
  assert.doesNotMatch(source, /MaxArchiveUncompressedBytes|MaxInputBytes|CopyWithLimitAsync/, `${path.relative(root, file)} reintroduced a fixed import byte cap.`);
}

console.log('Panel composer preview/templates, unlimited local-first imports, full FFmpeg workflow, Mainframe/VideoStudio hand-offs, and trim-relative video selection contracts passed.');
