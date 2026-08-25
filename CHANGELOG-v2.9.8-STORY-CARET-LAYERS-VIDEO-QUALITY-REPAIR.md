# PublisherStudio 2.9.8 — Story caret, layer drag and video-quality repair

## Interaction, stacking, input and frontend-failure release gate

- [x] **Canonical object-layer participation:** Mainframe layer drag/drop is additive UI over the existing `EditorStateService.SetSelectedLayerPosition(...)` operation. It does not create another Z-order model. Video recording placement uses the existing `IMediaTimelineEditService`/`TimelineEdits` sequence operations and does not mutate Mainframe geometry or object identity.
- [x] **Selection persistence and object operations:** selection persistence, move, resize, rotate, duplicate, delete and the four existing Front/Up/Down/Back layer-order operations remain unchanged. Drag/drop moves the already-selected layer block through the same normalized layer ordering path.
- [x] **Mouse, pen, touch, keyboard and controller/gamepad routing:** desktop pointer/mouse layer drag uses the browser's bounded HTML drag event and commits through the same editor state service. Existing Front/Up/Down/Back controls remain the keyboard/touch/controller-safe way to reorder without relying on native HTML drag support. No input-family-specific content model or service fork was added.
- [x] **Local stacking:** the layer drag marker is local to the Layers panel. No arbitrary application-wide maximum Z-index or new publication overlay was introduced.
- [x] **Preview, HTML/website, raster/SVG, print/PDF and video-render behavior:** Mainframe object order remains canonical for all existing render/export paths. Normal recorded/imported video continues to embed the original encoded media without a video transcode; WebM duration metadata may still be repaired when required. A full-range, no-effect, native-size Video Studio render now downloads the source bytes directly instead of re-encoding them; visual-effect baking preserves native dimensions, follows decoded source-frame cadence where the browser supports it, and does not deliberately reduce frame rate. Existing HTML, raster/SVG and print/PDF paths were not rewritten.
- [x] **Cleanup:** listener, pointer-capture, observer, object-URL and JavaScript interop cleanup remains owned by the existing Studio/runtime lifecycle. Story Editor no longer observes the RichEdit host's self-induced size changes; the shell observer is disconnected when its owner is gone. Render-export canvas tracks, audio nodes/context, video runtime and temporary DOM elements are cleaned in the maintained `finally` path.
- [x] **Structured diagnostics:** every changed recoverable component failure boundary uses its existing typed logger. Browser render/record failures continue through PublisherStudio JavaScript diagnostics; expected picker cancellation and an expected post-download circuit teardown are not promoted to alarming JavaScript errors.
- [x] **User notifications:** layer reorder, recording placement, media download and rendered-video failures surface through `IUserNotificationService`; expected permission-picker cancellation/circuit disposal is left to the existing operation state rather than generating duplicate alarm notifications.
- [x] **Regression evidence:** the supplied Edge trace repeatedly rejected `MediaCapabilities.encodingInfo({ type: "record" ... })`; the compatibility probe now memoizes that unsupported optional path and falls back to `MediaRecorder.isTypeSupported`. The supplied rendered WebM contained only 115 frames across roughly 28 seconds (about 4.17 FPS); source/native export no longer relies on the browser's unspecified canvas capture cadence. Story Editor resize dispatch is now guarded by an actual shell-width transition, preventing the RichEdit host → ResizeObserver → global resize → RichEdit reflow loop seen after DOCX download.

## Story Editor caret stability after DOCX download

The Story Editor layout bridge previously observed both the outer Story Editor shell and the DevExpress RichEdit host. RichEdit changes its own measurements when ribbon/export operations finish. Observing that host caused the following feedback path:

1. RichEdit changes host geometry.
2. The `ResizeObserver` schedules the PublisherStudio layout refresh.
3. PublisherStudio dispatches a global `resize` event.
4. RichEdit recalculates its viewport/caret and changes host geometry again.
5. The observer repeats the cycle.

The layout bridge now observes only the owning shell and dispatches a global resize only when the shell width actually changed. It also stops forcibly resetting the RichEdit host's horizontal scroll position and resolves the current RichEdit host after component rerenders instead of holding a stale host reference. DOCX download/ribbon operations may still schedule bounded layout checks, but those checks no longer become an endless caret/reflow loop.

## Mainframe layer drag/drop

The Layers panel now supports direct row drag/drop for arranging publication objects. A small drag handle identifies the interaction. The selected object—or the existing multi-selection block—is moved through `SetSelectedLayerPosition`, so normalized Z-order, selection and undo capture stay on the same editor-state path already used by the numeric layer position and Front/Up/Down/Back commands.

Dropping onto a row that is already part of the selected block is ignored. Existing buttons remain available, which matters on touch devices and other environments where native HTML drag/drop is not a reliable input primitive.

## Video recording placement and download

When a completed **video** recording exists while the Video Studio sequence already contains clips, PublisherStudio now asks where the new recording belongs before Apply can close the studio:

- first, before the current sequence;
- between any two existing clips; or
- last, after the current sequence.

The chosen boundary is projected onto the canonical timeline, then the existing `TimelineEdits.InsertAt(...)` machinery inserts a newly identified segment. The fresh recording deliberately does not inherit cut sections or effect layers from the clip that happened to be selected when recording started.

A completed recording may also be kept outside the sequence and downloaded. The Output command is no longer dependent solely on the temporary retained-recording Blob: when there is no retained recording but a selected embedded source is visible, **Download selected source** downloads that source instead.

## Video render quality and browser compatibility

### Exact source preservation when no visual bake is needed

For a full-source, 1× playback, unity-audio, native-dimension render with no visual effects, Video Studio now downloads the original media bytes directly. This avoids a needless MediaRecorder generation loss and preserves the source container, resolution and frame cadence. MP4, WebM, Ogg video and QuickTime source extensions are preserved when the browser reports the corresponding MIME type.

This also matches the Mainframe's normal media path: importing/applying a recording preserves its encoded video stream instead of transcoding it merely for display; PublisherStudio may repair WebM duration metadata without re-encoding the video frames.

### Source-frame-driven visual-effect baking

Visual-effect export still has to render pixels through a canvas. The old native/adaptive path could call `canvas.captureStream()` without a useful cadence, allowing the browser to choose a very low effective frame rate. 2.9.8 now:

- estimates decoded source cadence from `requestVideoFrameCallback` metadata when no manual FPS is requested;
- uses a zero-rate canvas track plus `CanvasCaptureMediaStreamTrack.requestFrame()` so one capture request follows each decoded/rendered source frame when that browser combination supports it;
- falls back to an explicit measured/source cadence rather than an unspecified canvas default when `requestFrame()` is unavailable;
- preserves native source dimensions in adaptive effect export;
- disables the adaptive frame-rate and resolution reduction steps for rendered Video Studio output;
- uses the source/fallback cadence in bitrate calculation instead of trusting a zero/low `captureStream` track setting; and
- raises the maintained explicit recording/render ceiling from 120 to 240 FPS while leaving native/source capture unconstrained by a forced width or height.

A visual-effect bake is still real-time browser encoding and therefore ultimately limited by the browser, codec, GPU/CPU and device throughput. 2.9.8 removes PublisherStudio's deliberate cadence/resolution downgrade and fixes the pathological low-frame-rate path; it does not claim that every browser can encode every 8K/240-FPS effect stack in real time.

### Edge `MediaCapabilities` error flood

The supplied Edge build exposes `navigator.mediaCapabilities.encodingInfo` but rejects the `record` encoding type with a `TypeError`. 2.9.7 retried that optional probe for every candidate codec, producing repeated console diagnostics during recording and rendered export.

2.9.8 isolates the probe behind one compatibility wrapper. Once that specific unsupported-enum behavior is observed, the optional probe is disabled for the page session and codec selection proceeds through `MediaRecorder.isTypeSupported` plus the existing adaptive bitrate policy. Unexpected capability-probe errors remain diagnosable.

User cancellation/denial of a camera/screen picker (`NotAllowedError`/`AbortError`) is similarly treated as an expected interactive outcome rather than duplicated as JavaScript failure noise.

## Render completion semantics

A rendered file that has already been encoded and downloaded is considered successful even if the subsequent Blazor completion callback races with circuit teardown/disposal. Expected disconnected/disposed callback errors are debug-level browser diagnostics; unexpected callback failures are still reported. This removes the confusing case where PublisherStudio displayed a render failure after the browser had already produced the file.

## Compatibility and preserved progress

- No publication schema migration was introduced.
- No Mainframe geometry, grouping, connector, animation, interaction or media-sequence ownership rule was replaced.
- Existing cut/range/effect editing remains the canonical media-edit model.
- Existing native audio/video control ownership and 2.9.6 signal-connector fixes remain unchanged.
- Local publication/Div template libraries from 2.9.6/2.9.7 remain unchanged.
- Routed InteractiveServer ownership remains unchanged; no nested component received its own render mode.
- DevExpress/DevExtreme remains **25.2.9** and the application continues to target **net10.0**.

## Version policy

PublisherStudio Web, InstallerConsole, npm metadata and browser cache markers are aligned at **2.9.8**. The single-digit minor/patch policy is satisfied. LocalGPT is not part of this source change and remains **3.3.0**.

## Validation status

This is a source-only release. The preparation environment intentionally does not run `dotnet`, MSBuild, NuGet restore/publish or licensed DevExpress compilation. The maintained architecture, async, component/service resilience, prerender-interoperability, iterator, Panel Studio persistence, XML/Razor documentation, JavaScript syntax/diagnostics and 2.9.8 release-contract audits are the source gate. The user's licensed Windows build remains authoritative for final Razor/C# compilation and runtime validation.
