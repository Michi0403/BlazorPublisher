# PublisherStudio 1.0.51

## Pointer ownership in exported publications

- Fixed stale DevExtreme chart hover and tooltip state when the pointer leaves a visual for another overlapping publication object. The supplied export demonstrated the issue: the browser correctly hit the higher-z video, but the previously hovered chart segment remained highlighted.
- Added one shared live-data pointer-ownership guard. It hides visual tooltips and clears series/point hover state whenever the active pointer belongs to a different publication surface, leaves the document, or the window loses focus.
- Exported data visuals, DevExtreme components, media, native controls, explicit publication interactions, and Signal Arrow click/hover source objects are retained as pointer owners.
- Non-interactive Shape, WordArt, and Barcode objects are pointer-passive during standalone presentation/site playback, allowing interactive content beneath transparent decorative bounds to receive the event.
- Added `data-element-kind` to authoring and print/export object markup so runtime behavior follows the publication element type instead of guessing from generated descendants.

## Application tooltip reliability

- Fixed application help tooltips repeatedly restarting their delay while the pointer crossed nested SVG paths, chart nodes, media controls, or component descendants inside the same publication object.
- Tooltip target resolution now follows the composed event path and keeps separate pending and active owners.
- Help tooltips use the browser Popover top layer when available, preventing publication-object and DevExpress stacking contexts from covering them.
- Retained viewport clamping and added an overlay-aware fallback z-index path for browsers without Popover support.
- Tooltips still close before pointer presses, clicks, context menus, scrolling, and Escape, and continue to support keyboard focus.

## Signal Arrow and workflow compatibility

- Signal Arrow/Connector document coordinates, path resolution, endpoint targeting, motion runners, trigger dispatch, chaining, and reset behavior are unchanged.
- Signal source objects configured for click or hover are explicitly excluded from pointer-passive handling.
- Publication JSON remains `1.45`; Picture Studio format remains `1.2`.
- Streaming, FFmpeg, recording, provider, LAN, device, installer, publication save/export, and single-application runtime workflows are unchanged.

## Validation

- Reproduced the stale-chart-hover problem in the supplied standalone HTML using headless Chromium.
- Verified that the v1.0.51 live-data runtime clears the old highlighted point and tooltip when the pointer enters the overlapping video object.
- Verified continuous pointer movement across nested SVG children still opens one object tooltip after the normal delay and that the tooltip is in the browser top layer.
- Added `pointerOwnership.test.mjs` covering tooltip ownership/top-layer contracts, exported pointer-owner classification, chart hover cleanup, and unchanged Signal Arrow geometry contracts.
- Every project JavaScript file passes `node --check`; all Node contract suites pass.
- A complete `dotnet restore`/`dotnet build` is not claimed in this packaging environment because the .NET 10 SDK and licensed DevExpress package feed are unavailable.
