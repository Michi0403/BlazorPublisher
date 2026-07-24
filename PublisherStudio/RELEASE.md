# PublisherStudio v1.0.68 release

See `CHANGELOG-v1.0.68.md`, `AGENTS.md`, ADR-009, `docs/ARCHITECTURE.md`, `docs/architecture/media-gesture-editing.md`, and `VALIDATION.md`.

This release adds explicit mouse/touch gesture modes and non-destructive section editing to Video Studio and Audio Studio. Timeline clicks synchronize the playhead and selected section; ribbon, context-menu, and scoped keyboard commands add/remove cutlines, copy/paste/delete sections, and insert compatible media into the selected range.

Video Studio additionally supports arbitrary normalized polygon frame regions. Picture Studio supports rectangle, ellipse, freehand, magnetic, and polygon area selections as layer clips, including inverted cuts and clipboard reuse. Audio remains intentionally one-dimensional.

All edits remain canonical content inside the existing media or picture element. The Mainframe remains the only publication-element insertion/update owner and preserves identity, placement, Z-order, grouping, connectors, animations, and interactions. The same sequence/region data is projected into editor preview, print/PDF, raster/SVG, interactive HTML, and standalone HTML.

Application and installer version is `1.0.68`. Publication format is `1.49`; Picture Studio format is `1.4`. No NuGet/npm/native dependency changed.
