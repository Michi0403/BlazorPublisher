# PublisherStudio v1.0.65 release

See `CHANGELOG-v1.0.65.md`, `AGENTS.md`, `docs/architecture/interchange-formats.md`, ADR-007 and `VALIDATION.md`.

This release establishes the first implemented open interchange adapters on PublisherStudio's canonical architecture. Picture Studio imports SVG/SVGZ as structured vector layers and OpenRaster as an ordered layered document. Source paths, visual leaves, named groups/layers, hidden state, local definitions, transforms and common OpenRaster stack properties are retained where the native model can represent them. Unsafe executable SVG content, DTD/entity expansion, online dependencies, unsafe archive paths and oversized/decompression-bomb input are rejected or reported.

The Path tool now places editable vector nodes instead of behaving like a freehand brush. WordArt supports picture and video fills through its glyph/path mask, with live video in the editor and interactive HTML plus deterministic poster/media snapshots for static export paths.

PublisherStudio additionally imports OpenDocument Drawing/Presentation packages and flat XML (`.odg`, `.odp`, `.fodg`, `.fodp`) into the native page system. Pages, common shapes, text frames, embedded images and retained SVG vector objects are mapped through a temporary model and compatibility report before commit.

The adapters remain under the owning reusable Services, shared results remain in Domain, and no new NuGet/npm/native dependency was introduced. Application and installer version is `1.0.65`; publication format is `1.48`; Picture Studio format is `1.3`.
