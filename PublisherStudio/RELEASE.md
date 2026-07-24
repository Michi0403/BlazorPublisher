# PublisherStudio v1.0.69 release

See `CHANGELOG-v1.0.69.md`, `AGENTS.md`, ADR-009, ADR-010, `docs/architecture/media-gesture-editing.md`, `docs/ARCHITECTURE.md`, and `VALIDATION.md`.

This release completes the spatial-selection workflow started in v1.0.68. Video Studio now owns frame-region gestures through a local overlay positioned over the actual contained video frame, above the native player controls. Picture Studio uses the same architectural principle through a pointer-transparent guide and canvas-local selection veil. Neither overlay participates in publication or picture-layer Z-order.

The recording finalization path also normalizes finite trim ranges before clamping, so a zero-length or sub-step browser recording cannot interrupt Stop Recording with an invalid `Math.Clamp` interval.

Application and installer version is `1.0.69`. Publication format remains `1.49`, Picture Studio format remains `1.4`, and dependencies are unchanged.
