# PublisherStudio v1.0.3 beta

- Replaced the SVG-foreignObject raster path with an html2canvas page renderer so PNG and JPEG export works with embedded images, audio cards, video poster/current frames, and multi-page ZIP output.
- Added browser-capture WebM presentation export, including page timing, animations, transitions, and playable embedded media when the selected browser capture includes tab audio.
- Added insertable QR Codes and Code 128, Code 39, EAN-13, UPC-A, ITF-14, and Codabar objects with foreground/background styling, QR error correction, and square/rounded/dot designs.
- Expanded Picture Studio with rectangle, ellipse, freehand, and magnetic selections plus solid and gradient selection fills.
- Rebuilt the Windows installer bootstrapper around GitHub release assets, per-user AppData installation, generated command launchers, and Start Menu actions for start, install, update, and uninstall.
- Added release packaging scripts and third-party notices for the browser-side raster and barcode libraries.
