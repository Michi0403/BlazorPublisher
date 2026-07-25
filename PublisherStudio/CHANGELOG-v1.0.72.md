# PublisherStudio v1.0.72 changelog

## Persistent clip selection and multiple cut sections

- The current video timestamp/range selection is now persisted on the selected clip instead of existing only as a temporary overlay value. Re-rendering, playhead updates, metadata reconciliation, layer edits, and clip switching no longer reset it to the trim boundary.
- A point selection and a range selection remain distinct. Numeric selection fields, the full-canvas overlay, project-sequence projection, playback, cutting, trimming, copying, and positioned video insertion use the same committed clip state.
- Each clip can now store multiple named cut sections. Saved sections can be selected, restored into the active range, enabled/disabled, and removed without replacing the current transient selection or splitting the clip immediately.
- Browser metadata can repair a placeholder `0.01 s` source duration while retaining an intentional committed selection whenever it still fits the resolved source duration.

## Layered live video effects

- Video clips now own an ordered `VideoEffectLayer` collection. Layers support visibility, locking, opacity, blend mode, optional source-time bounds, one normalized frame region, and an ordered live filter stack.
- Video Studio exposes layer creation, duplication, selection, reordering, renaming, visibility, opacity, blend mode, temporal bounds, and deletion in its ribbon/context/property workflow.
- Filters can be enabled, reordered, renamed, edited, and removed independently per layer.
- The initial filter set includes brightness, contrast, saturation, hue rotation, blur, grayscale, sepia, invert, chroma key, vignette, film grain, and color wash.
- Chroma key renders live with configurable key color, similarity, edge smoothness, spill reduction, and residual keyed opacity.

## Editable layer-bound frame regions

- Frame regions belong to the selected video layer rather than to one global video polygon.
- Entering Frame region mode loads that layer's region. Applying, clearing, copying, pasting, inverting, or dragging a region node commits only to that layer.
- Region points remain normalized source-frame coordinates and stay aligned after Stretch, Fill canvas, or Fit whole changes.
- The old `VideoElement.FrameClipPolygon` field remains as a compatibility projection of the first layer so older render/export paths can continue to read a single polygon.

## Shared rendering in Video Studio, Mainframe, and streaming inputs

- A shared browser canvas renderer now powers Video Studio effects, publication/Mainframe video preview, and visual live-source inputs.
- Publication videos render their selected clip's canonical layers and filters directly in the Mainframe instead of showing an unfiltered raw `<video>` element.
- Camera, screen, window, browser-tab, capture-device, and network video inputs use the same layer/filter renderer. Existing Inspector brightness, contrast, saturation, hue, blur, and chroma controls are projected into a canonical **Live input controls** layer while additional authored layers remain preserved.
- Visual live inputs now expose an Inspector editor for authored streaming-effect layers. Users can add, duplicate, reorder, hide, rename, blend, and change layer opacity, then add/reorder/toggle/edit the complete Video Studio filter set—including chroma key—without restarting capture.
- Live-source range sliders and color inputs now refresh the canonical control layer during the same edit, preventing stale streaming output after a Mainframe Inspector change.
- The renderer supports layer regions, inversion, temporal activation, opacity, blend modes, CSS-compatible adjustments, chroma key, color wash, vignette, and deterministic film grain without adding a third-party dependency.

## Persistence and compatibility

- Publication media normalization now deep-clones and validates selections, cut sections, video layers, frame regions, and filters through `MediaTimelineEditService`.
- Saving a Media Studio result preserves all nested clip state instead of flattening it to the old segment fields.
- Publications from older versions migrate their single frame polygon into the first video layer.
- Application and installer version is `1.0.72`.
- Publication format is `1.51`; Picture Studio format remains `1.4`.
- No NuGet, npm, native binary, or external-process dependency was added or changed.
