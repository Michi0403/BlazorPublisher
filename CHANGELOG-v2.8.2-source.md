# PublisherStudio 2.8.2 source changelog

## Mainframe interaction isolation while studios are open

- The publication mainframe now suspends its designer input contract whenever an overlay editor/studio is open, including Story, Spreadsheet, Picture, Data Visual, Component, Data Manager, Video/Audio Studio, Barcode, Streaming, Panel Studio, Media Converter, website export and Page Effect Studio surfaces.
- Selection visuals and mainframe interaction are controlled by the same existing editor visibility state. The page canvas remains mounted, so publication state, live media and studio hand-off state are not destroyed and recreated merely to block background editing.
- Publisher canvas pointer, double-click, context-menu, wheel, keyboard, paste, insertion and external-drop entry points now reject new work while interaction is suspended. An already-started pointer operation is cancelled safely on transition into a studio.
- No modal z-index hierarchy, publication element z-index, Picture Studio layer ordering or Video Studio layer ordering was rewritten. This is deliberately an interaction-boundary repair rather than another stacking-context redesign.

## Native slider drag stability

- Native `input[type=range]` controls remain browser-driven, but high-frequency `input` notifications are coalesced to the browser animation cadence before they cross the InteractiveServer circuit.
- The current value is flushed on `change`, pointer release/cancel, lost mouse-button state, window blur and document hiding so a drag cannot keep replaying a long queue of stale server events after the user lets go.
- No synthetic pointer movement, manual thumb positioning or per-monitor/per-machine timing constants were introduced.

## Media Converter Studio usability and adaptive defaults

- Converter Studio layout now uses responsive min/max grid tracks so the settings columns do not collide at narrower browser widths.
- Encoder preset and pixel-format fields now provide editable suggestions rather than expecting users to memorize FFmpeg tokens.
- Suggestion vocabulary is owned by `PublisherStudio.RuntimePolicy.Collections` in `appsettings.json`, not hard-coded in the Razor component.
- Media conversion presets can now carry configurable recommendations for encoder preset, pixel format, CRF and audio bitrate. Opening/changing a preset smart-fills missing/relevant values while every field remains user-editable.
- Existing detected FFmpeg encoder capabilities continue to populate the encoder datalist.
- A duplicate `InputKind` entry in the `audio-webm-opus` preset configuration was removed.

## Video Studio rendered-effects export

- Video Studio keeps its existing non-destructive HTML/canvas layer/effect graph and adds **Rendered video → Render selected range to video…**.
- Rendered export uses an off-screen video element and an exact-size canvas, applies the same `publisherVideoEffects` runtime used by live Video Studio preview, captures that canvas, mixes the source audio through Web Audio, and records the resulting stream with the existing adaptive browser recording policy.
- The exported file therefore contains the visible Video Studio effects while excluding browser chrome, player controls, PublisherStudio UI and editor selection overlays.
- Output dimensions, frame rate, codec choice, video bitrate and audio bitrate continue to come from the configurable adaptive recording layer and user overrides. Native source dimensions remain the fallback when no explicit target size is selected.
- Render cancellation and cleanup stop the recorder, source playback, capture tracks, effect runtime and audio graph without replacing the original media or flattening the editable Video Studio layers.
- The effect runtime no longer contains the previous fixed 1080p/4K-style canvas pixel cap. Explicit render dimensions win; optional caps remain configuration inputs rather than assumptions about one user's display.
- An existing double increment in the adaptive recording attempt loop was corrected.

## Compatibility retained

- Video Studio layer/effect editing remains intact; no effect or layer feature was removed.
- PublisherStudio's five reviewed `InteractiveServer` render boundaries are unchanged.
- LocalGPT 1-Wire protocol dependency remains `2.1.1`.
- Web and installer source versions are `2.8.2`; active PublisherStudio browser assets carry the 2.8.2 cache revision.

This is a source package. No .NET restore, build, publish or package command was run while preparing it.
