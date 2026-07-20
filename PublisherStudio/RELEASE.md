# PublisherStudio v1.0.40 release

See `CHANGELOG-v1.0.40.md`, `docs/COMPONENT_RUNTIME.md`, and `VALIDATION.md`.

Source release notes:

- Enlarged Text/Docx, Spreadsheet, Picture, Video, Audio, Data, Barcode, and Component Studio dialogs to use nearly the full browser viewport.
- Fixed dropdown-indicator spacing and centered the Component Studio close glyph.
- Added DevExtreme Map and Vector Map publication objects with publication-data, REST, OData, polling, print, presentation, and single-file website support.
- Added bundled World, Europe, Eurasia, Africa, USA, and Canada vector base maps.
- Added a Vector Map drawing editor for markers, lines, polygons, exact coordinates, feature styling, and GeoJSON import.
- Added persistent pan/zoom viewports for Text/Docx, Spreadsheet, Map, and Vector Map content.
- Added normalized custom CSS classes and safe inline CSS declarations for DevExtreme publication components.
- Publication format is now `1.38`; application/package/installer version is `1.0.40`.
- DevExtreme remains pinned to `25.2.8`.
- Run `Prepare-DevExpressAssets.cmd` on the licensed build machine before building or publishing.
- Run `npm run test:timeline` and `npm run test:components` from `src/PublisherStudio.Web` for the Node regression suites.
- A licensed end-to-end DevExpress build and browser test remain required on the development/release machine.
