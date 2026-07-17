# PublisherStudio v1.0.10 beta polish

- Reorganized the Publisher, Picture Studio, Media Studio, Story Editor, Barcode Studio, Publication Data, and timeline commands into categorized DevExpress ribbons.
- Restored readable ribbon command text, including Add Source, Barcode / QR, New Data Object, DOCX, and Update Fields.
- Restored direct, immediately clickable Media Studio transport, recording, and trim commands and fixed popup stacking inside studio windows.
- Hardened direct video/audio insertion and Media Studio imports with browser-file inspection, ranged local-media inspection, and a non-blocking editable duration fallback.
- Prevented canvas and media JS interop teardown from surfacing repeated JSDisconnected, TaskCanceled, and disposed-object exceptions.
- Reworked PNG and JPEG export around the DOM rasterizer, froze video frames before capture, capped oversized canvases, yielded between pages, and composited JPEG output onto white instead of producing black images.
- Changed SVG export to a fidelity-first embedded PNG surface so positioned, rotated, clipped, and oversized objects are not rearranged or cut off.
- Added PNG/SVG export for the currently selected object, suitable for overlays without exporting the full page.
- Improved website export by preserving computed typography/layout styles and keeping text and media individually selectable or right-clickable.
- Fixed QR square, rounded, and dot modules; exposed active correction/design details; restored barcode transparency; and improved validation for Code 39, EAN-13, UPC-A, ITF-14, and Codabar.
- Stabilized Barcode Studio with a fixed ribbon item tree, delayed generator readiness, and UPC-A checksum-safe examples.
- Routed Publisher Insert Video through the proven Media Studio import path and isolated selected-object raster export so padded crops no longer include neighboring objects.
- Disabled snap-to-grid by default for new publications and reduced the default grid spacing to 2.5 mm; object, guide, and page snapping remain available.
- Added snap-to-object movement with temporary green alignment, orange proximity, and red collision feedback.
- Added shape conversion commands for rectangle, rounded rectangle, ellipse, and line objects.
- Isolated printing from open File/export menus and application overlays.
