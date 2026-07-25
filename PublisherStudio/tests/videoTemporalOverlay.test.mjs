import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const mediaStudio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const timelineService = read('src', 'PublisherStudio.Web', 'Services', 'MediaStudio', 'UseCases', 'MediaTimelineEditService.cs');
const interop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

// Video uses a Studio-owned temporal overlay instead of the browser's opaque/fullscreen-prone controls.
assert.match(mediaStudio, /<video id="media-studio-preview"[\s\S]*playsinline[\s\S]*disablepictureinpicture[\s\S]*controlslist="nofullscreen nodownload noremoteplayback"/);
assert.doesNotMatch(mediaStudio, /<video id="media-studio-preview"[^>]*\scontrols(?:\s|>)/);
assert.match(mediaStudio, /id="media-studio-video-time-overlay"/);
assert.match(mediaStudio, /data-trim-start="@Inv\(_trimStart\)"/);
assert.match(mediaStudio, /data-selection-point="@\(_videoSelectionIsPoint/);
assert.match(mediaStudio, /data-video-time-handle="start"/);
assert.match(mediaStudio, /data-video-time-handle="end"/);
assert.match(mediaStudio, /Click for one timestamp · drag for a range/);
assert.match(mediaStudio, /@if \(!IsVideo\)[\s\S]*<DxRangeSelector/);

// A dropped video is staged, then positioned with a small slider inside the selected source range.
assert.match(mediaStudio, /class="media-video-insert-placement"/);
assert.match(mediaStudio, /Position inside selection/);
assert.match(mediaStudio, /The selected timestamp is fixed/);
assert.match(mediaStudio, /ConfirmPendingInsert/);
assert.match(mediaStudio, /MediaStudioDropInsertionPointSelected/);
assert.match(mediaStudio, /PreparePendingInsert/);
assert.match(mediaStudio, /SourcePositionToTimeline/);
assert.match(timelineService, /public Guid InsertAt\(/);
assert.match(timelineService, /var rightId = SplitAt/);

// Browser-local pointer movement owns the overlay and only commits bounded selection state to Blazor.
assert.match(interop, /function bindVideoTimeOverlay/);
assert.match(interop, /setPointerCapture/);
assert.match(interop, /VideoTimeSelectionCommitted/);
assert.match(interop, /normalizeVideoTimeRange/);
assert.match(interop, /video\.controls = false/);
assert.match(interop, /releaseVideoTimeOverlayBindings/);
assert.match(interop, /media-video-time-overlay/);
assert.match(interop, /MediaStudioDropInsertionPointSelected/);
assert.match(interop, /--video-drop-position/);

// The visual handles, selected shade, playhead and drop marker remain local to the video frame.
assert.match(css, /\.media-video-time-overlay\s*\{/);
assert.match(css, /\.media-video-time-handle/);
assert.match(css, /\.media-video-time-selected/);
assert.match(css, /\.media-video-time-playhead/);
assert.match(css, /\.media-video-time-drop-marker/);
assert.match(css, /\.media-video-insert-placement/);
assert.match(css, /touch-action:\s*none/);

console.log('PublisherStudio video temporal overlay, timestamp selection, and positioned drop insertion contracts passed.');
