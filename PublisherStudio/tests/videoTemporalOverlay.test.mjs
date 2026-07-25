import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const mediaStudio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const mediaModels = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationMediaModels.cs');
const publicationModels = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationModels.cs');
const editor = read('src', 'PublisherStudio.Web', 'Components', 'Pages', 'Editor.razor');
const timelineService = read('src', 'PublisherStudio.Web', 'Services', 'MediaStudio', 'UseCases', 'MediaTimelineEditService.cs');
const interop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

// Video uses a Studio-owned temporal overlay instead of the browser's opaque/fullscreen-prone controls.
assert.match(mediaStudio, /<video id="media-studio-preview"[\s\S]*playsinline[\s\S]*disablepictureinpicture[\s\S]*controlslist="nofullscreen nodownload noremoteplayback"/);
assert.doesNotMatch(mediaStudio, /<video id="media-studio-preview"[^>]*\scontrols(?:\s|>)/);
assert.match(mediaStudio, /id="media-studio-video-time-overlay"/);
assert.match(mediaStudio, /data-trim-start="@Inv\(_trimStart\)"/);
assert.match(mediaStudio, /data-selection-point="@\(_videoSelectionIsPoint/);
assert.match(mediaStudio, /data-clip-name="@SelectedClipName"/);
assert.match(mediaStudio, /data-segment-timeline-start="@Inv\(SelectedSegmentTimelineStart\)"/);
assert.match(mediaStudio, /data-mouse-mode="@_mouseMode"/);
assert.match(mediaStudio, /class="media-video-time-toolbar media-video-transport-dock"/);
assert.match(mediaStudio, /id="media-studio-sequence-playhead"/);
assert.match(mediaStudio, /<DxRibbonGroup Text="Pointer mode"/);
assert.match(mediaStudio, /<DxRibbonGroup Text="Video canvas"/);
assert.match(mediaStudio, /Pointer: select timestamp\/range/);
assert.match(mediaStudio, /Canvas: stretch/);
assert.match(mediaStudio, /data-video-time-handle="start"/);
assert.match(mediaStudio, /data-video-time-handle="end"/);
assert.match(mediaStudio, /Click for one timestamp; drag for a selected range/);
assert.match(mediaStudio, /@if \(!IsVideo\)[\s\S]*<DxRangeSelector/);

// Selection values are editable, stay tied to the active sequence clip, and drive real clip operations.
assert.match(mediaStudio, /id="media-studio-video-selection-mode"/);
assert.match(mediaStudio, /id="media-studio-video-selection-start"/);
assert.match(mediaStudio, /id="media-studio-video-selection-end"/);
assert.match(mediaStudio, /ChangeVideoSelectionMode/);
assert.match(mediaStudio, /SetVideoSelectionRange/);
assert.match(mediaStudio, /id="media-studio-sequence-selection"/);
assert.match(mediaStudio, /private void CutVideoSelection\(\)/);
assert.match(mediaStudio, /TimelineEdits\.SplitAt\(_segments, _playbackRate, endTimeline\)/);
assert.match(mediaStudio, /TimelineEdits\.SplitAt\(_segments, _playbackRate, startTimeline\)/);
assert.match(mediaStudio, /private void UseVideoSelectionAsTrim\(\)/);
assert.match(mediaStudio, /private void CopyVideoSelection\(\)/);
assert.match(mediaStudio, /Dropped video is inserted only inside this selection/);

// The play canvas exposes all persisted video fit modes, including non-proportional stretch.
assert.match(mediaStudio, /Video inside play canvas/);
assert.match(mediaStudio, /VideoFitStretch/);
assert.match(mediaStudio, /PublicationVideoFitMode\.Stretch => "fill"/);
assert.match(mediaStudio, /VideoFitMode = _videoFitMode/);
assert.match(mediaModels, /PublicationVideoFitMode VideoFitMode/);
assert.match(editor, /video\.FitMode = result\.VideoFitMode/);
assert.match(publicationModels, /FitMode \{ get; set; \} = PublicationVideoFitMode\.Stretch/);
assert.match(publicationModels, /bool FitModeExplicit/);
assert.match(mediaStudio, /video\.FitModeExplicit \|\| video\.FitMode != PublicationVideoFitMode\.Contain/);
assert.match(editor, /video\.FitModeExplicit = true/);

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
assert.match(interop, /VideoPlayheadCommitted/);
assert.match(interop, /VideoCutlineCommitted/);
assert.match(interop, /function videoCanvasMode/);
assert.match(interop, /function setVideoPlayheadVisual/);
assert.match(interop, /media-studio-sequence-playhead/);
assert.match(interop, /timeOverlay\.style\.width = `\$\{Math\.max\(1, stageBounds\.width\)\}px`/);
assert.match(interop, /normalizeVideoTimeRange/);
assert.match(interop, /syncVideoSelectionControls/);
assert.match(interop, /media-studio-video-selection-mode/);
assert.match(interop, /media-studio-video-selection-start/);
assert.match(interop, /media-studio-sequence-selection/);
assert.match(interop, /project timestamp/);
assert.match(interop, /getComputedStyle\(video\)\.objectFit/);
assert.match(interop, /export function refreshMediaStudioOverlay/);
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
assert.match(css, /\.media-sequence-selection/);
assert.match(css, /\.media-video-selection-value/);
assert.match(css, /touch-action:\s*none/);
assert.match(css, /v1\.0\.71 video play-canvas overlay orchestration/);
assert.match(css, /\.media-studio-preview-shell\.video \.media-studio-video-surface[\s\S]*max-height:\s*none/);
assert.match(css, /\.media-video-time-overlay \{[\s\S]*inset:\s*0/);
assert.match(css, /\.media-video-time-toolbar\.media-video-transport-dock[\s\S]*bottom:\s*0/);
assert.match(css, /\.media-studio-frame-overlay-host \{ z-index:\s*20/);
assert.match(css, /\.media-studio-frame-overlay-host:not\(\.active\) \{[\s\S]*visibility:\s*hidden/);
assert.match(css, /Insert into the selected clip at the highlighted timestamp/);

console.log('PublisherStudio full-canvas video overlay, bottom transport, pointer modes, clip orchestration, and positioned drop contracts passed.');
