# PublisherStudio v1.0.70 changelog

## Managed component drops

- Dropping an image onto an existing publication picture now opens that picture as a managed Picture Studio project and imports the dropped image as a new editable layer instead of creating an unrelated page object.
- Picture Studio accepts positioned raster, SVG/SVGZ and OpenRaster drops and preserves the local canvas drop point when creating imported layers.
- Dropping compatible video or audio onto an existing media component now opens the owning Studio and stages the dropped source as a sequence insert. Incompatible media still follows the normal page-insertion path.
- Mainframe drop routing now carries the target element identity, target kind and target-local coordinates without transferring Studio ownership into the page surface.

## Video temporal orchestration

- Video Studio now uses a Studio-owned timestamp/range overlay directly over the rendered video frame. Native browser video controls and fullscreen-prone click behavior are disabled for the editor preview.
- Click selects one source timestamp; drag selects a source range; start/end handles adjust an existing range. The interaction is bounded to the selected clip's current trim range.
- The selected timestamp/range is projected into the currently selected project segment and highlighted on the sequence timeline. Selecting another sequence clip reloads that clip's source, trim bounds and temporal selection.
- Selection type, start, end and summary fields update with overlay gestures. The numeric fields are editable and drive the same canonical selection state.
- A point selection can add one cutline. A range can create boundaries around the area, become the selected clip trim, or be copied as a reusable sequence section.
- Playing a video selection uses the selected source timestamp/range rather than the unrelated audio volume control or audio range selector.

## Positioned video insertion

- Dropping a video inside the temporal overlay uses the pointer position as the initial insertion timestamp.
- A range selection opens a compact insertion-position slider constrained to that selected source range. A point selection uses its single timestamp directly.
- Confirmation splits the existing sequence at the projected timeline position and inserts the dropped clip there without replacing the selected source clip.

## Video play-canvas fit

- Video Studio now exposes **Fit whole**, **Fill canvas**, and **Stretch** modes for the source inside the play canvas.
- The temporal and frame-region overlays are recalculated against the rendered source rectangle after fit changes, including letterboxing, crop overflow and non-proportional stretch.
- The selected fit mode is saved back to the publication video and remains available in editor, preview, print and export renderers.

## Reliability and compatibility

- Browser-local pointer movement updates temporary overlay visuals while Blazor receives committed, bounded selection state.
- All new overlays remain transient Studio UI and never become publication siblings, picture layers or media sequence segments.
- Application and installer version is `1.0.70`.
- Publication format remains `1.49`; Picture Studio format remains `1.4`.
- No NuGet, npm or native dependency was added or changed.
