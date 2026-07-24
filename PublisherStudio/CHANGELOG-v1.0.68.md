# PublisherStudio v1.0.68

## Media Studio gesture editing

- Video Studio and Audio Studio now expose explicit **Select section**, **Place playhead**, and **Add cutline** mouse/touch modes.
- Pressing the sequence timeline places the playhead, selects the corresponding section, and seeks the active source. The active mode, playhead, selected section, cutlines, and section boundaries are visible.
- Ribbon and context commands add or remove cutlines, copy/paste sections, delete the selected section, and insert a compatible media file into the selected timeline range.
- Delete, Ctrl/Cmd+C, Ctrl/Cmd+V, Enter, and Escape are scoped to the open Studio and ignore text-entry controls.
- A reusable `MediaTimelineEditService` owns deterministic split, merge, selection mapping, cloning, and sequence normalization. The editable sequence is persisted inside one publication media element.

## Two-dimensional video and picture regions

- Video Studio adds a video-only **Frame region** mode. Mouse or touch vertices define a normalized polygon that supports arbitrary source dimensions and angles.
- Picture Studio adds polygon selection and converts rectangle, ellipse, freehand, magnetic, and polygon selections into non-destructive layer clip polygons.
- Picture selections can be kept, inverted as a cut-out, copied, or copied as a new clipped layer through the existing Picture Studio clipboard.
- Picture raster rendering and SVG export both retain normal and inverted layer clips. Audio Studio intentionally remains one-dimensional.

## Mainframe and export integration

- Edited sequences and frame regions are returned through canonical Domain result contracts and applied by the existing Mainframe insert/edit orchestration.
- Updating media content preserves the publication object's identity, geometry, Z-order, group membership, connectors, animations, and interactions.
- Removed segment assets are released and current sequence assets are registered directly, avoiding stale media-store entries and incorrect preview reuse.
- Mainframe preview, print/PDF, raster/SVG projection, interactive HTML, and standalone HTML read the same media sequence and video frame polygon.

## Architecture and compatibility

- Added `docs/architecture/media-gesture-editing.md` and ADR-009.
- Repository rules now enforce one gesture owner, scoped/disposable listeners, editor-local overlays, temporal/spatial coordinate separation, Mainframe-owned insertion, and Z-order preservation.
- Publication format is `1.49`; Picture Studio format is `1.4`.
- Application and installer version is `1.0.68`.
- No NuGet, npm, native binary, or external-process dependency was added.
