import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const models = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationModels.cs');
const surface = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PageSurface.razor');
const editor = read('src', 'PublisherStudio.Web', 'Components', 'Pages', 'Editor.razor');
const pictureEditor = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor.cs');
const pictureMarkup = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor');
const mediaStudio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const publisherInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'publisherInterop.js');
const pictureInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'pictureStudioInterop.js');
const mediaInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

assert.match(models, /Guid\? TargetElementId = null,[\s\S]*double TargetX = \.5,[\s\S]*double TargetY = \.5/);
assert.match(surface, /data-element-x="@Inv\(element\.X\)"/);
assert.match(surface, /data-element-rotation="@Inv\(element\.Rotation\)"/);
assert.match(surface, /CompleteExternalFileDrop[\s\S]*targetElementId[\s\S]*targetX[\s\S]*targetY/);

assert.match(publisherInterop, /function compatibleExternalDropTarget/);
assert.match(publisherInterop, /kind === 'picture' && targetKind === 'image'/);
assert.match(publisherInterop, /kind === 'video' && targetKind === 'video'/);
assert.match(publisherInterop, /kind === 'audio' && targetKind === 'audio'/);
assert.match(publisherInterop, /external-file-component-drop-target/);
assert.match(publisherInterop, /target\?\.id \|\| '', target\?\.kind \|\| '', target\?\.x \?\? \.5, target\?\.y \?\? \.5/);
assert.match(publisherInterop, /return 'audio'/);

assert.match(editor, /target is ImageFrameElement image/);
assert.match(editor, /OpenPictureStudioWithInsertedRaster/);
assert.match(editor, /PictureDocuments\.AddRasterLayer/);
assert.match(editor, /target is PublicationMediaElement media/);
assert.match(editor, /OpenMediaStudio\(media\.Kind, media, segment\)/);
assert.match(editor, /InitialInsertedSegment="_mediaStudioInitialInsertedSegment"/);
assert.match(mediaStudio, /\[Parameter\] public PublicationMediaSegment\? InitialInsertedSegment/);
assert.match(mediaStudio, /_segments\.Add\(normalizedInsert\)/);

assert.match(pictureMarkup, /picture-studio-image-drop-input/);
assert.match(pictureMarkup, /picture-studio-layer-drop-input/);
assert.match(pictureEditor, /ImportDroppedImage/);
assert.match(pictureEditor, /forceAdd: true/);
assert.match(pictureEditor, /PictureStudioFileDropPositioned/);
assert.match(pictureEditor, /AddImportedLayers/);
assert.match(pictureInterop, /bindPictureDrop/);
assert.match(pictureInterop, /canvasPoint\(canvas, event\)/);
assert.match(mediaInterop, /bindMediaDrop/);
assert.match(css, /\.pub-element\.external-file-component-drop-target/);
assert.match(css, /\.picture-studio-dialog\.picture-file-drag-active/);
assert.match(css, /\.media-studio-window\.media-file-drag-active/);

console.log('Managed PictureStudio and media-component drop routing contracts passed');
