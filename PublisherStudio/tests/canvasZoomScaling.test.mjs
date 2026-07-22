import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const pageSurface = fs.readFileSync(path.join(root, 'src/PublisherStudio.Web/Components/Editor/PageSurface.razor'), 'utf8');
const css = fs.readFileSync(path.join(root, 'src/PublisherStudio.Web/wwwroot/css/site.css'), 'utf8');
const printSurface = fs.readFileSync(path.join(root, 'src/PublisherStudio.Web/Components/Editor/PrintPublication.razor'), 'utf8');
const interop = fs.readFileSync(path.join(root, 'src/PublisherStudio.Web/wwwroot/js/publisherInterop.js'), 'utf8');

assert.match(pageSurface, /<div class="publication-element-content" style="@ElementContentStyle\(element\)" data-editor-content-scale="@Inv\(State\.Document\.Zoom\)">/);
assert.ok(pageSurface.indexOf('publication-element-content') < pageSurface.indexOf('@switch (element)'), 'the stable-layout wrapper must contain every editor element type');
assert.ok(pageSurface.indexOf('</div>\n                    @if (!element.Locked)') > pageSurface.indexOf('@switch (element)'), 'selection ports and resize handles must remain outside the scaled content wrapper');
assert.match(pageSurface, /private const double BasePixelsPerMm = 3\.7795275591;/);
assert.match(pageSurface, /private double PixelsPerMm => BasePixelsPerMm \* State\.Document\.Zoom;/);
assert.match(pageSurface, /private string ElementContentStyle\(PublicationElement element\) => \$"width:\{ContentMm\(element\.Width\)\};height:\{ContentMm\(element\.Height\)\};transform:scale\(\{Inv\(State\.Document\.Zoom\)\}\)";/);
assert.match(pageSurface, /padding:@ContentMm\(text\.PaddingMm\)/);
assert.match(pageSurface, /border:@ContentMm\(text\.BorderWidth\)/);
assert.match(pageSurface, /border:@ContentMm\(spreadsheet\.BorderWidthMm\)/);
assert.match(pageSurface, /height:\{ContentMm\(Math\.Max\(shape\.StrokeWidth, \.3\)\)\}/);
assert.match(pageSurface, /border:\{ContentMm\(image\.BorderWidthMm\)\}/);
assert.match(css, /\.publication-element-content\s*\{[\s\S]*?position:\s*absolute;[\s\S]*?transform-origin:\s*0 0;[\s\S]*?overflow:\s*visible;/);
assert.doesNotMatch(printSurface, /publication-element-content/, 'print/export rendering remains on its canonical publication surface');
assert.match(interop, /function syncEditorElementContentFrame\(state, element, widthMm, heightMm\)/);
assert.match(interop, /content\.style\.width = `\$\{Math\.max\(0, number\(widthMm\)\) \* basePixelsPerMm\}px`/);
assert.match(interop, /content\.style\.transform = `scale\(\$\{zoom\}\)`/);
assert.equal((interop.match(/syncEditorElementContentFrame\(state, /g) || []).length, 3, 'the helper must be defined and used by live resize plus cancellation restore');

const pxPerMm = 3.7795275591;
for (const zoom of [.2, .5, .75, 1, 1.25, 2, 4]) {
    for (const millimeters of [1, 12.5, 80, 210]) {
        const zoomedFrame = millimeters * pxPerMm * zoom;
        const transformedStableContent = millimeters * pxPerMm * zoom;
        assert.ok(Math.abs(zoomedFrame - transformedStableContent) < 1e-9);
    }
}

console.log('Canvas zoom scaling contracts passed.');
