# BlazorPublisher v1.0.8 — interaction and export stabilization

## Fixed

- Publication selection, movement, resizing and double-click activation now use one deterministic pointer state machine instead of competing Blazor click handlers.
- Clicking the page background clears selection without making the next object selection flash and disappear.
- Stale mouse-down operations are cancelled on lost capture, pointer cancellation, browser blur and tab visibility changes.
- Picture Studio applies the same pointer release and drag-threshold rules, including tool changes and area selections.
- Video Studio can insert newly imported or newly recorded media directly into the publication again and reuses the already-decoded preview asset.
- Raster export converts CSS Color 4 values such as `color(display-p3 ...)` to ordinary RGBA values before html2canvas parses them.
- PNG and JPEG export first use browser foreign-object rendering, retry with the sanitized computed renderer, and retain SVG rasterization as a final fallback.
- PNG, JPEG, SVG and standalone HTML export use the page's physical dimensions at the canonical 96-DPI CSS scale rather than the editor's current zoom.
- Standalone HTML pages store explicit canonical pixel dimensions so fitting remains stable across browser window sizes.
- Hidden export pages now expose their width and height in millimetres for deterministic export sizing.
