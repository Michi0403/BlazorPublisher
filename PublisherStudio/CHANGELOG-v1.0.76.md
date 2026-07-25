# PublisherStudio v1.0.76 changelog

## VideoStudio interaction persistence

- Fixed temporal selections disappearing when the mouse button, pen, or touch contact is released.
- Pointer-up, pointer-cancel, and lost-pointer-capture now commit the same visible selection to the selected clip.
- Added an explicit persisted `TemporalSelectionCommitted` flag so late browser metadata and ordinary rerenders cannot replace a user-selected range with the clip trim range.
- Browser-reported source duration now repairs placeholder `0.01 s` clips before the selection is committed, while preserving an already committed timestamp or range.
- Selection start/end fields, the play-canvas overlay, sequence highlight, saved cut sections, and selected clip now use the same canonical range.

## Layer timing and filters

- Added explicit **Selection → layer / Apply to layer** commands in the ribbon, Inspector, play-canvas workflow, and context menu.
- Renamed the unrelated saved-range command to **Save as cut section** so cut sections and layer timing are no longer presented as the same operation.
- Applying a selection stores its temporal range directly on the selected video layer and refreshes the live renderer.
- Layer, frame-region, and filter mutations now request a live effect refresh, including chroma-key color and threshold edits.
- Removed a duplicate anonymous-object property in the video-layer JS payload that could cause a C# compilation failure after the earlier Razor error was fixed.

## Playback and Blazor Server stability

- The play-canvas transport buttons now execute through a browser-local handler, preserving the native user gesture and avoiding an unnecessary Blazor Server round trip.
- Play, pause, seek, scrub, recording, and disposal share one cancellable playback command state.
- Expected browser `AbortError` rejections such as “The play() request was interrupted by a call to pause()” are resolved as cancellation instead of escaping as `Microsoft.JSInterop.JSException` or a `RemoteRenderer` failure.
- Ribbon playback remains available through C#↔JS interop and now receives a non-throwing result from the browser bridge.

## Persistence and release alignment

- Added the persisted clip-selection ownership field and advanced publication format from `1.52` to `1.53`.
- Older publications remain compatible; normalization infers committed selections from existing point selections and non-full-range selections.
- Advanced web application, installer, npm package, lock file, streaming runtime capability, and structured-export manifest versions to `1.0.76`.
- Dependency names and versions are unchanged.
