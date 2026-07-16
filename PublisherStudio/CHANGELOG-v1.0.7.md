# BlazorPublisher v1.0.7 — deterministic pointer and media insertion fix

## Fixed

- Publication selection is now controlled by the pointer state machine instead of competing Blazor click handlers.
- Clicking outside the page clears selection without causing a selected object to flash and immediately lose focus.
- Single clicks no longer create no-op move/resize commits or snap objects to the grid from tiny pointer jitter.
- Move and resize operations use a movement threshold and commit only after a real drag.
- Pointer-up and pointer-cancel fallbacks run at window level, preventing stuck mouse-down and repeated click loops.
- Double-click activation is dispatched directly from the pointer state machine, so text, picture, chart, media and barcode editing remain reliable after selection rerenders.
- Picture Studio now releases pointer capture deterministically and applies the same drag threshold to layer movement and resizing.
- Switching Picture Studio tools cancels any stale canvas operation before changing modes.
- Video Studio insertion now has an explicit applying state, normalizes the result before insertion and closes the studio before the publication rerenders.
- Media Studio reuses its already-decoded ranged preview asset when adding or replacing publication media, avoiding another large base64 decode during insertion.
- Server-side transform commits ignore unchanged bounds, preventing needless undo entries and rerenders.
