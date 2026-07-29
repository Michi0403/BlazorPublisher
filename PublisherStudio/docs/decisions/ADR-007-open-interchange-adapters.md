# ADR-007: Open interchange adapters use canonical models and BCL parsers

## Status

Accepted for PublisherStudio v1.0.65.

## Context

Picture Studio and the Publisher page system need editable exchange with common, openly specified formats. PublisherStudio must remain an offline-first monolith, preserve its own architecture, and must not gain a new package dependency merely to mirror another application's object model.

## Decision

- PublisherStudio's native publication and picture documents remain authoritative.
- Import adapters live below the owning reusable Service namespace:
  - `Services/PictureStudio/Import`
  - `Services/Publication/Import`
- Components only select files, show compatibility results, and commit a successfully validated canonical document.
- Shared import results and compatibility issues live in `Domain`.
- SVG and SVGZ are parsed/sanitized into `SvgPictureLayer` records. Visual leaf paths/shapes/text/images retain their SVG markup, paint servers, transforms, masks and clipping definitions; source layer/group names are retained as metadata.
- OpenRaster is read with `System.IO.Compression` and `System.Xml.Linq`. Its uppermost-first stack is converted to Picture Studio's bottom-to-top render order. Nested stack names remain in `GroupPath` even where group compositing must be flattened.
- OpenDocument Drawing/Presentation packages (`.odg`, `.odp`) and flat XML variants (`.fodg`, `.fodp`) are read with BCL ZIP/XML APIs and mapped to PublisherStudio pages, text frames, pictures, basic shapes and retained SVG vector objects.
- No new NuGet or JavaScript package is introduced for these adapters.
- Unsafe executable SVG features and external online asset references are removed or rejected because imported projects must remain offline and deterministic.
- Unsupported or approximated features produce an explicit `InterchangeIssue` rather than silently changing the canonical model.

## Consequences

PublisherStudio gains practical editable interchange without making SVG, OpenRaster or ODF its internal architecture. The first import surface is intentionally conservative: advanced foreign effects may be retained as sanitized SVG, approximated, or reported as a compatibility loss. Future adapters must extend the same result/report contract rather than introducing a generic endpoint or a new architectural root.
