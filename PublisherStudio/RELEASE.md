# PublisherStudio v1.0.41 release

See `CHANGELOG-v1.0.41.md`, `docs/ANIMATION_EXPORT.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENT_RUNTIME.md`, and `VALIDATION.md`.

This release adds self-contained SVG output and editable path nodes to Picture Studio, plus offline-capable Signal Arrows and Signal Connectors for interactive and recorded publications.

Signal connectors can:

- connect objects or arbitrary page coordinates;
- run on page entry, click, hover, or manual preview;
- emit click/hover gestures at both endpoints;
- animate inner map, vector-map, spreadsheet, and text content;
- highlight spreadsheet cells and HTML/chart parts through local CSS selectors;
- show, hide, fade, animate, control media, apply CSS classes, and chain to another signal;
- run in the editor preview, single-file presentation/site exports, and video export.

Standalone HTML contains the signal runtime and all signal configuration. It does not require a PublisherStudio server. Network-backed live data naturally still needs its configured endpoint to be reachable.

Versions: application/package/installer `1.0.41`, publication format `1.39`, picture format `1.2`.
