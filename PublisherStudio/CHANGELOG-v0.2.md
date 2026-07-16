# v0.2 canvas, image, menu, and export pass

## Added

- DevExpress page/object context menu.
- Adaptive canvas rulers linked to page rectangle, zoom, and scrolling.
- Ruler units: millimetres, centimetres, inches, and pixels.
- Drag-from-ruler guide creation, guide movement, guide removal, grid and snap settings.
- Page settings inspector and expanded preset catalogue.
- Live picture properties and discoverable crop workflow.
- Picture rotation independent from frame rotation.
- Copy/paste within the publication.
- PNG, JPEG, SVG, website, and print/PDF commands.
- Configurable raster-export DPI.
- Fit-page command and improved status-bar zoom controls.

## Fixed

- File inputs now reset before opening, allowing the same picture to be selected again for replacement.
- Ruler cursor markers no longer render from a null pointer position.
- Previous fixed rulers were replaced by live canvas rulers.
- JSON save is now clearly separated from output/export commands.

## Compatibility

- Host, installer, controllers, routing, DI, and project boundaries are retained.
- No new NuGet dependency was introduced.
- Publication files from the first format remain readable through defaulted v1.1 properties.
