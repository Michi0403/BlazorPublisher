import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const mediaModels = read('src', 'PublisherStudio.Web', 'BusinessObjects', 'PublicationMediaModels.cs');
const publicationModels = read('src', 'PublisherStudio.Web', 'BusinessObjects', 'PublicationModels.cs');
const pictureModels = read('src', 'PublisherStudio.Web', 'BusinessObjects', 'PictureStudioModels.cs');
const timelineService = read('src', 'PublisherStudio.Web', 'Services', 'MediaStudio', 'UseCases', 'MediaTimelineEditService.cs');
const mediaStudio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const editor = read('src', 'PublisherStudio.Web', 'Components', 'Pages', 'Editor.razor');
const surface = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PageSurface.razor');
const print = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PrintPublication.razor');
const mediaInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');
const publisherInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'publisherInterop.js');
const pictureInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'pictureStudioInterop.js');
const pictureEditor = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor');
const pictureEditorCode = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor.cs');
const pictureState = read('src', 'PublisherStudio.Web', 'Services', 'PictureEditorStateService.cs');
const editorState = read('src', 'PublisherStudio.Web', 'Services', 'EditorStateService.cs');
const program = read('src', 'PublisherStudio.Web', 'Program.cs');
const applicationComposition = read('src', 'PublisherStudio.Web', 'PublisherStudioServiceCollectionExtensions.cs');
const agents = read('AGENTS.md');
const architecture = read('docs', 'architecture', 'media-gesture-editing.md');
const adr = read('docs', 'decisions', 'ADR-009-editor-gesture-and-z-order-ownership.md');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

const pictureModule = await import(pathToFileURL(path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'js', 'pictureStudioInterop.js')).href);
const clippedSvg = await pictureModule.createPictureStudioSvg({
    name: 'Clipped shape',
    widthPx: 100,
    heightPx: 100,
    background: 'transparent',
    layers: [{
        id: 'clip-shape', kind: 'Shape', name: 'Shape', visible: true, opacity: 1, blendMode: 'Normal',
        x: 50, y: 50, width: 80, height: 80, rotation: 0, shape: 'Rectangle', fillKind: 'Solid',
        fillColor: '#ff0000', secondaryFillColor: '#ffffff', strokeColor: 'transparent', strokeWidthPx: 0,
        clipPolygon: [{ x: 10, y: 10 }, { x: 90, y: 10 }, { x: 50, y: 90 }], clipInverted: true
    }]
});
assert.match(clippedSvg, /clipPathUnits="userSpaceOnUse"/);
assert.match(clippedSvg, /clip-rule="evenodd"/);
assert.match(clippedSvg, /clip-path="url\(#ps-clip-shape-0-area-clip\)"/);
assert.match(clippedSvg, /M 0 0 H 100 V 100 H 0 Z M 10 10 L 90 10 L 50 90 Z/);

// Canonical, serialized media edits stay inside one publication element.
assert.match(mediaModels, /enum MediaStudioMouseMode \{ SelectSection, PlacePlayhead, AddCutLine, FrameRegion \}/);
assert.match(mediaModels, /sealed class PublicationMediaSegment/);
assert.match(mediaModels, /List<PublicationMediaSegment> Segments/);
assert.match(mediaModels, /List<MediaFramePoint> FrameClipPolygon/);
assert.match(publicationModels, /FormatVersion \{ get; set; \} = "1\.55"/);
assert.match(publicationModels, /public List<PublicationMediaSegment> Segments/);
assert.match(publicationModels, /public List<MediaFramePoint> FrameClipPolygon/);
assert.match(editor, /item\.Segments = result\.Segments\?\.Select/);
assert.match(editor, /video\.FrameClipPolygon = result\.FrameClipPolygon\?\.Select/);
assert.match(editor, /previousSegmentIds\.UnionWith\(media\.Segments/);
assert.match(editor, /previousSegmentIds\.Except\(currentSegmentIds\)/);
assert.match(editor, /foreach \(var segment in media\.EffectiveSegments\) MediaAssets\.GetOrRegister\(segment\)/);

// Reusable service owns deterministic timeline mutations; the Component owns transient gesture state.
assert.match(timelineService, /namespace PublisherStudio\.Services\.MediaStudio\.UseCases/);
assert.match(timelineService, /Guid\? SplitAt/);
assert.match(timelineService, /bool MergeBoundary/);
assert.match(timelineService, /PublicationMediaSegment Duplicate/);
assert.match(applicationComposition, /AddSingleton<MediaTimelineEditService, MediaTimelineEditService>/);
assert.match(mediaStudio, /@inject MediaTimelineEditService TimelineEdits/);
assert.match(mediaStudio, /MouseModeText\(MediaStudioMouseMode\.SelectSection/);
assert.match(mediaStudio, /MouseModeText\(MediaStudioMouseMode\.PlacePlayhead/);
assert.match(mediaStudio, /MouseModeText\(MediaStudioMouseMode\.AddCutLine/);
assert.match(mediaStudio, /MouseModeText\(MediaStudioMouseMode\.FrameRegion/);
assert.match(mediaStudio, /TimelinePointerDown/);
assert.match(mediaStudio, /Text="Cut at playhead" Enabled="@CanAddCutLine" Click="AddCutLineAtPlayhead"/);
assert.match(mediaStudio, /Text="Remove cutline" Enabled="@CanRemoveCutLine" Click="RemoveCutLine"/);
assert.match(mediaStudio, /Add cutline at playhead/);
assert.match(mediaStudio, /Remove cutline before selected section/);
assert.match(mediaStudio, /Copy selected section/);
assert.match(mediaStudio, /Paste section after selection/);
assert.match(mediaStudio, /Delete selected section/);
assert.match(mediaStudio, /Insert media into selected range/);
assert.match(mediaStudio, /private bool CanDeleteSection => _segments\.Count > 0 && HasSelectedSegment/);
assert.match(mediaStudio, /if \(_segments\.Count == 0\)[\s\S]*ClearSelectedSegmentFields\(\)/);
assert.match(mediaStudio, /MediaStudioShortcutRequested/);
assert.match(mediaStudio, /if \(!IsVideo && mode == MediaStudioMouseMode\.FrameRegion\)/);

// Video alone has normalized two-dimensional polygon editing.
assert.match(mediaStudio, /id="media-studio-frame-overlay-host"/);
assert.match(mediaStudio, /class="media-studio-frame-overlay"/);
assert.match(mediaStudio, /class="media-frame-dim"/);
assert.match(mediaStudio, /FrameOverlayPointerDown/);
assert.match(mediaStudio, /FrameDimPath/);
assert.match(mediaStudio, /CancelFrameRegion/);
assert.match(mediaStudio, /normalizedPoint/);
assert.match(mediaStudio, /FrameClipCss/);
assert.match(mediaStudio, /Math\.Sqrt\(x \* x \+ y \* y\)/);
assert.doesNotMatch(mediaStudio, /Math\.Hypot/);
assert.match(css, /\.media-studio-frame-overlay-host\.active/);
assert.match(css, /\.media-frame-cursor-ring/);
assert.match(css, /box-shadow:\s*0 0 0 100vmax/);
assert.match(css, /pointer-events:\s*none/);
assert.match(css, /touch-action:\s*none/);

// Keyboard handling is modal-root scoped and disposed.
assert.match(mediaInterop, /root\.contains\(event\.target\)/);
assert.match(mediaInterop, /HTMLInputElement/);
assert.match(mediaInterop, /document\.removeEventListener\("keydown", state\.keyboardHandler, true\)/);
assert.match(mediaInterop, /studioStates\.delete\(id\)/);

// Mainframe and every established output projection receive sequence sources and frame clipping.
assert.match(surface, /data-media-segment/);
assert.match(print, /data-media-segment/);
assert.match(surface, /VideoMediaStyle/);
assert.match(print, /VideoMediaStyle/);
assert.ok((publisherInterop.match(/data-media-segment/g) || []).length >= 2, 'Editor and standalone runtimes must both read media segments.');
assert.match(publisherInterop, /configurePublicationMedia/);
assert.match(publisherInterop, /configureMediaSegment/);
assert.match(publisherInterop, /segment\.start/);
assert.match(publisherInterop, /segment\.end/);

// Picture Studio supports arbitrary polygon overlay cuts and clipboard reuse.
assert.match(pictureModels, /PictureDrawTool \{[^}]*PolygonSelect/);
assert.match(pictureModels, /List<PicturePoint> ClipPolygon/);
assert.match(pictureModels, /bool ClipInverted/);
assert.match(pictureModels, /FormatVersion \{ get; set; \} = "1\.4"/);
assert.match(pictureInterop, /"polygonselect"/);
assert.match(pictureInterop, /getPictureStudioAreaSelection/);
assert.match(pictureInterop, /drawSelectionModeVeil/);
assert.match(pictureInterop, /ctx\.fill\("evenodd"\)/);
assert.match(pictureInterop, /selection-gesture-active/);
assert.match(pictureEditor, /picture-studio-gesture-guide/);
assert.match(pictureInterop, /ctx\.clip\(layer\.clipInverted === true \? "evenodd" : "nonzero"\)/);
assert.match(pictureInterop, /function svgLayerClip/);
assert.match(pictureInterop, /clipPathUnits="userSpaceOnUse"/);
assert.match(pictureInterop, /clip-rule="evenodd"/);
assert.match(pictureInterop, /disposePictureStudio/);
assert.match(pictureInterop, /removeEventListener\("pointerdown"/);
assert.match(pictureEditor, /@PolygonSelectToolText/);
assert.match(pictureEditor, /Keep selected area/);
assert.match(pictureEditor, /Cut selected area/);
assert.match(pictureEditor, /Copy selected area as layer/);
assert.match(pictureEditorCode, /SelectionPolygon/);
assert.match(pictureEditorCode, /Enumerable\.Range\(0, 48\)/);
assert.match(pictureEditorCode, /ApplyAreaClipAsync/);
assert.match(pictureEditorCode, /CopyAreaSelectionToClipboardAsync/);
assert.match(pictureState, /CopySelectedRegion/);
assert.match(pictureState, /ApplySelectedClip/);
assert.match(pictureState, /ClearSelectedClip/);
assert.match(pictureState, /"free" or "magnetic" or "polygon" => PictureShapeKind\.Freeform/);

// Segment asset cleanup and architecture/Z-order safeguards are explicit.
assert.match(editorState, /foreach \(var segment in media\.Segments\) _mediaAssets\.Remove\(segment\.Id\)/);
assert.match(agents, /A media sequence, cutline, temporal section or frame\/picture region is canonical content inside the owning media or picture element/);
assert.match(agents, /Video region overlays must align to the actual rendered source-frame rectangle/);
assert.match(agents, /Never call `Math\.Clamp\(value, min, max\)` unless `min <= max` is guaranteed/);
assert.match(agents, /never mutate Mainframe layer order/);
assert.match(agents, /A gesture may have exactly one owner/);
assert.match(agents, /Keyboard shortcuts must be scoped to the active Studio root/);
assert.match(agents, /Every persisted visual edit must be covered in Mainframe preview, print\/PDF, raster\/SVG export, interactive HTML and standalone HTML/);
assert.match(architecture, /Internal media sections and selection overlays never become page siblings or picture layers/);
assert.match(adr, /never become publication elements or Z-order participants/);

console.log('PublisherStudio media gesture, polygon region, sequence, export and Z-order contracts passed.');
