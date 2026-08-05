import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const mediaStudio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const mediaInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');
const pictureEditor = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PictureEditor.razor');
const pictureInterop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'pictureStudioInterop.js');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');
const agents = read('AGENTS.md');
const architecture = read('docs', 'articles', 'architecture.md');
const adr = read('docs', 'articles', 'architecture.md');

// Video's spatial overlay owns input only while its explicit mode is active.
assert.match(mediaStudio, /media-studio-frame-overlay-host @\(_mouseMode == MediaStudioMouseMode\.FrameRegion \? "active"/);
assert.match(mediaStudio, /@onpointerdown="FrameOverlayPointerDown"/);
assert.match(mediaStudio, /@onpointerdown:preventDefault/);
assert.match(mediaStudio, /@onpointerdown:stopPropagation/);
assert.match(mediaStudio, /normalizedPoint",\s*"media-studio-frame-overlay-host"/);
assert.match(mediaStudio, /Apply region/);
assert.match(mediaStudio, /Undo point/);
assert.match(mediaStudio, /Exit mode/);
assert.match(mediaStudio, /ActiveFramePoints\.Count >= 3/);
assert.match(css, /\.media-studio-frame-overlay-host\s*\{[\s\S]*pointer-events:\s*none/);
assert.match(css, /\.media-studio-frame-overlay-host\.active\s*\{[\s\S]*pointer-events:\s*auto/);
assert.match(css, /cursor:\s*none/);
assert.match(css, /\.media-frame-dim\s*\{\s*fill:/);

// Browser-local synchronization follows the actual contained video rectangle and is disposed.
assert.match(mediaInterop, /function syncFrameOverlay/);
assert.match(mediaInterop, /video\.videoWidth/);
assert.match(mediaInterop, /Math\.min\(availableWidth \/ sourceWidth, availableHeight \/ sourceHeight\)/);
assert.match(mediaInterop, /new ResizeObserver/);
assert.match(mediaInterop, /releaseFrameOverlayBindings/);
assert.match(mediaInterop, /frameResizeObserver\?\.disconnect/);
assert.match(mediaInterop, /--media-pointer-x/);

// Picture selection feedback is transient and pointer-transparent.
assert.match(pictureEditor, /id="picture-studio-canvas-surface"/);
assert.match(pictureEditor, /class="picture-studio-gesture-guide"/);
assert.match(css, /\.picture-studio-gesture-guide\s*\{[\s\S]*pointer-events:\s*none/);
assert.match(pictureInterop, /function drawSelectionModeVeil/);
assert.match(pictureInterop, /function drawAreaSelection/);
assert.match(pictureInterop, /ctx\.fill\("evenodd"\)/);
assert.match(pictureInterop, /areaSelectionHandlePoints/);
assert.match(pictureInterop, /updatePictureGesturePointer/);
assert.match(pictureInterop, /selectionMode:\s*isAreaSelectionTool/);

// Recording finalization cannot create an invalid clamp interval.
assert.match(mediaStudio, /private \(double Start, double End\) NormalizeTrimRange/);
assert.doesNotMatch(mediaStudio, /private static \(double Start, double End\) NormalizeTrimRange/);
assert.match(mediaStudio, /var minimumSpan = Math\.Min\(\.01, safeDuration\)/);
assert.match(mediaStudio, /var maximumStart = Math\.Max\(0, safeDuration - minimumSpan\)/);
assert.match(mediaStudio, /var minimumEnd = Math\.Min\(safeDuration, safeStart \+ minimumSpan\)/);
assert.match(mediaStudio, /RangeSelectorMinRange => Math\.Min\(\.05, RangeSelectorEnd\)/);
assert.doesNotMatch(mediaStudio, /Math\.Clamp\(Math\.Max\(values\[0\], values\[1\]\), _trimStart \+ \.01, _duration\)/);

// Architecture rules make the local overlay and Z-order ownership explicit.
assert.match(agents, /Editor interaction overlays are transient frontend projections, not canonical content/);
assert.match(agents, /Do not use an application-wide Z-index to solve a local editor problem/);
assert.match(agents, /High-frequency pointer movement stays in browser JavaScript\/CSS/);
assert.match(architecture, /local positioned stacking context/);
assert.match(adr, /never change publication layer order/);

console.log('PublisherStudio local Studio overlay, source-frame alignment and short-recording range contracts passed.');
