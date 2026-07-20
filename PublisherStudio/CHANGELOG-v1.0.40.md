# PublisherStudio v1.0.40 — larger studios, maps, vector drawing, and movable content viewports

## Larger application studios

- Text/Docx, Spreadsheet, Picture, Video, Audio, Data, Barcode, and Component Studio dialogs now use nearly the full available viewport.
- The Spreadsheet data-object dialog also has a substantially larger working area.
- Responsive minimum sizes no longer force studios beyond narrow browser windows.
- Native select controls reserve enough room for their dropdown indicator.
- Ribbon dropdown indicators are kept in normal layout flow so they no longer overlap command text.
- The Component Studio close glyph is centered inside its button.

## DevExtreme Map and Vector Map

- Added Map and Vector Map to the insert catalogue and Component Studio.
- Map supports Google, Google Static, and Azure providers, map type, center, zoom, controls, automatic marker fitting, API keys, address/coordinate fields, markers, grouped routes, and ordered route points.
- Vector Map supports bundled World, Europe, Eurasia, Africa, USA, and Canada base layers plus a blank drawing surface.
- Both map controls use the same publication-data, REST, OData, polling, editor-preview, presentation-export, and single-file website runtime as the existing application components.
- Live refresh updates map markers/routes and vector-map layers without replacing the publication object.

## Vector Map editor

- Added click-to-draw markers, lines, and polygons in the live Component Studio preview.
- Drawing points are stored as precise longitude/latitude coordinates and can be edited numerically.
- Features support names, labels, fill/stroke colors, opacity, line width, marker size, and values.
- Added finish, undo-point, remove-feature, and clear-drawing commands.
- Added GeoJSON import for Point, LineString, Polygon, MultiLineString, and MultiPolygon geometries.

## Movable content viewports

- Text/Docx frames, Spreadsheet frames, Map, and Vector Map objects can now reposition the visible content inside the publication object.
- Enter **Position visible content/map** from the context menu or Component Tools, drag to pan, and use the mouse wheel to zoom.
- Content offset and scale are stored with the document, participate in undo/redo, and are preserved in print, presentation, and website output.
- Fit/fill/stretch calculations remain active while the additional user viewport transform is applied.
- Reset commands restore the original content position and scale.

## Component CSS

- Component Studio now accepts a custom CSS class and safe inline CSS declarations for every DevExtreme publication component.
- CSS classes and declarations are normalized before save/export; script-like CSS and selector blocks are rejected.

## Compatibility and validation

- Application/package version updated to `1.0.40`.
- Publication document format updated from `1.37` to `1.38` for stored content viewports, map configuration, and vector features.
- DevExtreme remains pinned to `25.2.8`.
- Existing documents remain compatible; new fields use backward-compatible defaults.
