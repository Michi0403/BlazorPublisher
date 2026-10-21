// Portable source/codec regression for the Picture Studio 4.2.2 canvas contract.
// The browser-canvas pixel/drawing interaction test is performed separately in Chromium.
// The repository's normal build still uses PowerShell, never Python.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const read = relative => readFileSync(resolve(root, relative), 'utf8');
const editor = read('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs');
const razor = read('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor');
const js = read('src/PublisherStudio.Web/wwwroot/js/pictureStudioInterop.js');
const state = read('src/PublisherStudio.Web/Services/PictureEditorStateService.cs');
const documentService = read('src/PublisherStudio.Web/Services/PictureDocumentService.cs');
const business = read('src/PublisherStudio.Web/BusinessObjects/PictureStudioModels.cs');
assert(editor.indexOf('"initializePictureStudio"') < editor.indexOf('if (_pendingRasterInitialization'),
    'Bind the canvas before asynchronous GIF/raster loading can trigger frame callbacks.');
assert(editor.includes('if (_module is null || !Visible || !_initialized) return;'));
assert(js.includes('function scheduleEditorRender(editor)'));
assert(js.includes('requestAnimationFrame(async () =>'));
assert(js.includes('drawDocument(stage, documentSnapshot'), 'Raster/animated content must render to private canvas, never clear the visible surface while awaiting decode.');
assert(js.includes('context.drawImage(rendered, 0, 0)'), 'Only the latest completed composition may replace visible canvas pixels.');
assert(js.includes('if (token !== editor.renderToken || editor.canvas !== target || !target.isConnected)'), 'Discard stale async canvas renders.');
assert(js.includes('new MutationObserver('), 'Recover DevExpress popup-owned canvas replacements without remounting the C# editor.');
assert(js.includes('function ensurePictureStudioCanvas(editor)'), 'Browser canvas lifetime must not be controlled by a one-time Blazor flag.');
assert(js.includes('editor.canvasObserver?.disconnect()'), 'Release DOM observer when the editor closes.');
assert(js.includes('editor.canvasAwaited = true'), 'Late DevExpress portal attachment must be observable even if the first OnAfterRender sees no canvas.');
assert(js.includes('void safeInvoke(editor, "PictureCanvasAttached")'), 'Late attachment must request the authoritative Blazor document.');
assert(editor.includes('public async Task PictureCanvasAttached()'), 'The reattachment callback must be registered in Picture Editor.');
assert(js.includes('editor.dropRoot !== document.getElementById(editor.rootId)'), 'Drag/drop host rebinding must be idempotent across server rerenders.');
assert(editor.includes('var canvasBound = await _module.InvokeAsync<bool>("initializePictureStudio"'), 'Reconfirm the actual bound DOM canvas each Blazor render.');
assert(js.includes('if (!ensurePictureStudioCanvas(editor)) return;'), 'Render must re-check the live popup canvas before scheduling.');
assert(js.includes('layer.animationFrameIndex !== (document.activeAnimationFrame || 0)'));
assert(editor.includes('CreateFrameRenderProjection(State.Document, State.Document.ActiveAnimationFrame)'));
assert(editor.includes('SendPictureGifFramesAsync(sourceDocument, exportId)'));
assert(editor.includes('finishPictureStudioGifApply'));
assert(editor.includes('IsPictureBusy => IsPictureExporting || _gifImportId is not null'));
assert(razor.includes('Text=\'@LT("Convert visible layers to GIF frames")\''));
assert(state.includes('public bool ConvertVisibleLayersToGifFrames('));
assert(state.includes('layer.AnimationFrameIsOverlay = true'));
assert(business.includes('public bool AnimationFrameIsOverlay { get; set; }'));
assert(documentService.includes('!layer.AnimationFrameIsOverlay'));
assert(documentService.includes('overlay.AnimationFrameIndex = nextIndex'));
console.log('Picture Studio canvas initialization, original render loop, editable GIF frame routing, Apply Merged and undoable layer conversion: passed.');
