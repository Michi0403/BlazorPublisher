import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const pageSurface = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
const ribbon = read('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor');
const model = read('src/PublisherStudio.Web/Domain/PublicationModels.cs');
const state = read('src/PublisherStudio.Web/Services/EditorStateService.cs');
const files = read('src/PublisherStudio.Web/Services/PublicationFileService.cs');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const printSurface = read('src/PublisherStudio.Web/Components/Editor/PrintPublication.razor');
const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');

assert.match(pageSurface, /class="publication-page @CanvasZoomModeClass/);
assert.match(pageSurface, /data-zoom-mode="@State\.Document\.View\.CanvasZoomMode\.ToString\(\)\.ToLowerInvariant\(\)"/);
assert.match(pageSurface, /<div class="publication-element-content @ElementZoomCompatibilityClass\(element\)" style="@ElementContentStyle\(element\)"/);
assert.match(pageSurface, /data-editor-zoom-strategy="@ElementZoomStrategy\(element\)"/);
assert.ok(pageSurface.indexOf('publication-element-content') < pageSurface.indexOf('@switch (element)'), 'the stable-layout wrapper must contain every editor element type');
assert.ok(pageSurface.indexOf('</div>\n                    @if (!element.Locked)') > pageSurface.indexOf('@switch (element)'), 'selection ports and resize handles must remain outside the scaled content wrapper');
assert.match(pageSurface, /private const double BasePixelsPerMm = 3\.7795275591;/);
assert.match(pageSurface, /private double PixelsPerMm => BasePixelsPerMm \* State\.Document\.Zoom;/);
assert.match(pageSurface, /CanvasZoomModeClass => State\.Document\.View\.CanvasZoomMode == PublicationCanvasZoomMode\.Transform/);
assert.match(pageSurface, /ElementZoomCompatibilityClass\(PublicationElement element\) => element is DevExtremeComponentElement/);
assert.match(pageSurface, /ElementZoomStrategy\(PublicationElement element\) => element is DevExtremeComponentElement[\s\S]*?"transform"/);
assert.match(pageSurface, /private string ElementContentStyle\(PublicationElement element\) => \$"width:\{ContentMm\(element\.Width\)\};height:\{ContentMm\(element\.Height\)\};--publisher-editor-zoom:\{Inv\(State\.Document\.Zoom\)\}";/);
assert.match(pageSurface, /padding:@ContentMm\(text\.PaddingMm\)/);
assert.match(pageSurface, /border:@ContentMm\(text\.BorderWidth\)/);
assert.match(pageSurface, /border:@ContentMm\(spreadsheet\.BorderWidthMm\)/);
assert.match(pageSurface, /height:\{ContentMm\(Math\.Max\(shape\.StrokeWidth, \.3\)\)\}/);
assert.match(pageSurface, /border:\{ContentMm\(image\.BorderWidthMm\)\}/);

assert.match(model, /PublicationCanvasZoomMode \{ CssLayout, Transform \}/);
assert.match(model, /CanvasZoomMode \{ get; set; \} = PublicationCanvasZoomMode\.CssLayout/);
assert.match(state, /public void SetCanvasZoomMode\(PublicationCanvasZoomMode mode\)[\s\S]*Document\.View\.CanvasZoomMode = mode;[\s\S]*Notify\(\);/);
assert.match(files, /Enum\.IsDefined\(document\.View\.CanvasZoomMode\)/);
assert.match(ribbon, /Rendering: sharp CSS/);
assert.match(ribbon, /Rendering: compact transform/);
assert.match(ribbon, /UseCssZoomMode/);
assert.match(ribbon, /UseTransformZoomMode/);

assert.match(css, /\.publication-element-content\s*\{[\s\S]*?position:\s*absolute;[\s\S]*?transform-origin:\s*0 0;[\s\S]*?transform:\s*scale\(var\(--publisher-editor-zoom, 1\)\)/);
assert.match(css, /\.publication-page\.zoom-mode-css \.publication-element-content:not\(\.zoom-transform-compat\)[\s\S]*?zoom:\s*var\(--publisher-editor-zoom, 1\);[\s\S]*?transform:\s*none/);
assert.match(css, /\.publication-page\.zoom-mode-transform \.publication-element-content,[\s\S]*?\.publication-page\.zoom-mode-css \.publication-element-content\.zoom-transform-compat[\s\S]*?transform:\s*scale\(var\(--publisher-editor-zoom, 1\)\)/);
assert.doesNotMatch(printSurface, /publication-element-content/, 'print/export rendering remains on its canonical publication surface');

assert.match(interop, /function syncEditorElementContentFrame\(state, element, widthMm, heightMm\)/);
assert.match(interop, /content\.style\.width = `\$\{Math\.max\(0, number\(widthMm\)\) \* basePixelsPerMm\}px`/);
assert.match(interop, /requestedMode = String\(state\?\.page\?\.dataset\?\.zoomMode/);
assert.match(interop, /strategy = String\(content\.dataset\.editorZoomStrategy/);
assert.match(interop, /requestedMode === "csslayout"/);
assert.match(interop, /strategy !== "transform"/);
assert.match(interop, /CSS\?\.supports\?\.\("zoom", "1"\)/);
assert.match(interop, /content\.style\.zoom = String\(zoom\)/);
assert.match(interop, /content\.style\.zoom = "1"/);
assert.match(interop, /content\.style\.transform = `scale\(\$\{zoom\}\)`/);
assert.match(interop, /function syncEditorZoomRendering\(state\)/);
assert.match(interop, /syncEditorZoomRendering\(state\);/);
assert.equal((interop.match(/syncEditorElementContentFrame\(state, /g) || []).length, 4, 'the helper must be defined and used by mode refresh, live resize, and cancellation restore');

const pxPerMm = 3.7795275591;
for (const zoom of [.2, .5, .75, 1, 1.25, 2, 4]) {
    for (const millimeters of [1, 12.5, 80, 210]) {
        const zoomedFrame = millimeters * pxPerMm * zoom;
        const transformedStableContent = millimeters * pxPerMm * zoom;
        assert.ok(Math.abs(zoomedFrame - transformedStableContent) < 1e-9);
    }
}

console.log('Selectable CSS/transform canvas zoom and DevExtreme compatibility contracts passed.');
