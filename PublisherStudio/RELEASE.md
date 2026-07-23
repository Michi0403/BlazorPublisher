# PublisherStudio v1.0.61 release

See `CHANGELOG-v1.0.61.md`, `CHANGELOG-v1.0.60.md`, and `VALIDATION.md`.

The mainframe canvas zoom control now uses deterministic percentages. Range dragging updates the native control locally and commits the publication zoom only when the gesture finishes, avoiding the previous feedback loop where a Blazor canvas rerender changed scroll geometry while the pointer was still down. Plus/minus use exact 5% steps, an editable percentage field supports direct values, and a dedicated 100% reset is available.

Both **Sharp CSS layout** and **Compact transform** continue to render live DOM/SVG/text from the stable authored-content wrapper. CSS layout zoom participates in layout and rasterization; transform mode composites after layout, but Chromium/Edge can rerasterize live vector glyphs at the current device pixel ratio. This explains why both can appear crystal sharp on UHD hardware while retaining a small quality difference.

Application and installer version `1.0.61`; publication format `1.47`; picture format `1.2`. There is no separate Media Host executable or release payload.
