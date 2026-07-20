# PublisherStudio v1.0.38 — stable video/animation timeline playback and trimming

## Fixed runaway timeline playback

- Fixed the Video Cut / publication timeline playhead accelerating after repeated Play, Pause, Stop, scrub, or reopen operations.
- Playback now owns a unique run identifier. Delayed JavaScript-to-Blazor callbacks from an older run are ignored instead of moving the current playhead.
- Animation-frame callbacks verify the exact active playback state rather than only checking whether a page has *some* playback state. A dequeued stale callback can therefore no longer restart itself or stop a newer run.
- Starting playback atomically replaces the previous run, and closing the timeline explicitly stops the page runtime before the JavaScript module is disposed.

## Bounded playhead updates

- Playhead notifications now use backpressure: only one JS-to-.NET update may be in flight, while intermediate positions collapse to the newest value.
- Natural completion still delivers the final position, while Pause and Stop invalidate pending updates immediately.
- This prevents a slow render or browser tab from building a growing callback queue that later appears to race through the timeline.

## Video-cut and clip-drag safety

- Timeline clip pointer movement is clamped to the visible track rectangle before time deltas are calculated. Pointer capture outside the editor can no longer produce unbounded trim lengths or invalid CSS percentages.
- Animation and media clip commits reject non-finite positions and durations on both the browser and Blazor sides.
- The Video Studio trim selector and numeric controls reject `NaN`/infinite values while retaining the existing non-destructive trim model, source-duration limits, playback rate, fades, looping, and cut calculations.
- Moving clips, trimming either edge, scrubbing, selected-range playback, and page-duration expansion remain unchanged for valid input.

## Validation and compatibility

- Added a Node regression test covering stale animation-frame callbacks, repeated playback replacement, JS interop backpressure, natural completion, and extreme pointer movement during media trimming.
- Application/package version updated to `1.0.38`.
- DevExpress and DevExtreme remain pinned to `25.2.8`.
- Publication document format remains `1.36`; no stored publication schema changed in this release.
