# PublisherStudio 2.9.9 — Video sequence and capture-preset repair

## Interaction, stacking, input and frontend-failure release gate

- [x] **Canonical object-layer participation:** repeated Video Studio recordings now enter the existing `MediaTimelineEditService` sequence through the canonical insertion path; no parallel clip-order model was introduced and Mainframe object identity/order is untouched.
- [x] **Selection and layer operations:** recording insertion does not mutate Mainframe move/resize/rotate/duplicate/delete or Front/Up/Down/Back behavior. The 2.9.8 layer-row drag implementation remains unchanged.
- [x] **Input routing:** the new placement controls are ordinary Studio-local buttons/selects and do not add document/global pointer or keyboard listeners. Existing mouse, pen, touch and keyboard gesture ownership remains unchanged.
- [x] **Local stacking:** the before/after/between chooser reuses the existing Video Studio insertion overlay and local stacking context; no application-wide maximum z-index was added.
- [x] **Preview/export behavior:** recording presets affect browser capture constraints only. Inserted recordings remain canonical video segments used by Mainframe preview, website/HTML, print/export and Video Studio rendering through the existing media pipeline.
- [x] **Cleanup:** the browser still owns and revokes retained recording object URLs. A retained Blob is released for the next capture only after PublisherStudio knows the previous recording has been committed to the sequence, or when the Studio is closed/disposed.
- [x] **Diagnostics and recoverable failures:** changed component paths retain structured `ILogger<MediaStudio>` diagnostics and `IUserNotificationService` notifications. Browser capability fallbacks remain non-fatal.
- [x] **Regression coverage:** `build/audit_release_2_9_9.py` asserts first-recording protection, pre-capture sequence placement, retained-Blob commit state, canonical insertion, fractional FPS preservation, capture presets, localization parity, previous 2.9.8 caret/layer/video fixes and JavaScript diagnostics hashes.

## Why 2.9.8 could still lose the previous recording

2.9.8 added a placement chooser, but its guard deliberately returned when the canonical sequence contained zero clips. The first screen/camera capture therefore remained only as the browser-retained recording. Starting a second capture caused the browser recording runtime to release that retained Blob as part of starting the new `MediaRecorder`, so the new recording appeared to replace the old one.

The server logs from the reproduced case confirmed that successive recordings were successfully retained with different sizes and durations while no canonical sequence insertion occurred between them. The failure was therefore sequence ownership, not `MediaRecorder` capture failure.

## Recording sequence repair

- Every completed video recording is now protected by an explicit canonical sequence-placement step, including the very first recording (`Only video`).
- PublisherStudio will not silently start another video capture while a completed recording is still retained but not committed to the sequence.
- Once at least one sequence clip exists, pressing **Record camera** or **Record screen** first opens a placement chooser. The user can select:
  - **First** / before the current first clip;
  - any **Between** boundary;
  - **Last** / after the current last clip.
- The chosen boundary survives the browser capture-picker/start transition and is preselected when the new recording completes.
- **Insert recording** uses the existing `MediaTimelineEditService.InsertAt` projection and creates a fresh segment without inheriting the selected clip's trim/effects.
- The retained browser recording now carries a `committedToSequence` state. Re-render/reconnect metadata enrichment cannot accidentally make an already-inserted recording look uncommitted again.
- Keeping a recording outside the sequence remains possible for download, but attempting another video recording reopens the placement requirement instead of destroying it.
- Audio recording behavior is intentionally unchanged.

## Video capture controls expanded from Panel / Div Studio concepts

Video Studio now exposes a practical capture-dimension palette instead of only Source, Streaming master/output and Custom:

- Source/native;
- 8K UHD, 4K UHD, DCI 4K, UWQHD, QHD, Full HD and HD;
- vertical 4K / vertical Full HD;
- square 2K / square Full HD;
- the maintained **Panel / Div Studio viewport presets**, including user-defined presets that fit the runtime media limits;
- existing Streaming master/output presets;
- Custom width and height.

The selection is an *ideal browser capture constraint*, not a promise that a browser/OS/display can manufacture pixels its capture source does not expose. PublisherStudio reports the actual capture-track width, height and frame rate returned by the browser after recording starts.

Frame-rate selection now includes source/native plus 23.976, 24, 25, 29.97, 30, 48, 50, 59.94, 60, 90, 120, 144, 165, 240 and a fractional custom value. JavaScript normalization no longer truncates 23.976/29.97/59.94 to integers. In adaptive mode a selected ceiling is also passed as the ideal capture-frame-rate request, while `0` continues to mean source/native cadence.

Manual bitrate and codec controls remain available; adaptive quality continues to select within the repository's runtime policy. The maintained ceiling remains 7680×4320 and 240 FPS, while Source/native leaves dimensions/cadence to the selected browser source.

## Quality findings retained from 2.9.8

The supplied recordings confirmed why the earlier rendering repair mattered: a native VP9 screen recording was 3828×1962 at 30 FPS, whereas an older rendered result at the same dimensions contained roughly 4.17 video frames per second. 2.9.8's decoded-frame-driven render path remains intact in 2.9.9; this release does not revert to browser-default zero-rate canvas capture.

The second supplied source recording is variable/sparse in its WebM timing metadata, which is valid for browser screen capture: PublisherStudio must preserve the captured frame timing rather than inventing a fixed lower rate merely because container header metadata is unusual.

## Browser reconnect messages

The Edge `Page entered Back-Forward Cache` / WebSocket 1006 messages in the reproduction are browser navigation/circuit reconnect behavior, not the recording replacement mechanism. PublisherStudio already reconnects the Blazor circuit afterward. This release therefore does not add an unrelated transport workaround to Video Studio.

## Compatibility

- Native publication and media models remain backward compatible.
- No new package or process dependency was added.
- DevExpress remains pinned to 25.2.9.
- PublisherStudio version advances from 2.9.8 to 2.9.9.
- LocalGPT is unchanged at 3.3.0.
