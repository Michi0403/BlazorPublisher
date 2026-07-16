# BlazorPublisher v0.4 custom WordArt paths

- Added `Custom` as a WordArt transform without removing Straight, Arch Up, Arch Down, or Wave.
- Added a compact path canvas to the WordArt properties panel.
- Added eight path presets: straight, rising, falling, arch up, arch down, gentle wave, S curve, and circular arc.
- Added freehand path drawing. The stroke is simplified into a small set of editable points instead of storing raw pointer noise.
- Added draggable path points with distinct start and end markers.
- Added path reversal, point reduction, text position along the path, and distance from the path.
- Added **Draw path** and **Reverse path** commands to the contextual WordArt ribbon and right-click menu.
- Stored custom paths as normalized publication data (`WordArtPathPoint`) rather than untrusted SVG strings.
- Reused the same WordArt renderer for the live canvas, print surface, website export, SVG export, and PNG/JPEG raster pipeline.
- Increased the publication format version to 1.5. Existing publication files remain compatible.
- Added no dependencies and made no host, controller, service-boundary, or InstallerConsole changes.
