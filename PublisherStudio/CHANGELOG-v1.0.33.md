# PublisherStudio v1.0.33 — Transform and workbook workflow completion

## Spreadsheet Studio

- Removed PublisherStudio's artificial file-upload ceilings from browser file streams, ASP.NET Core form handling, Kestrel request bodies, publication/assets endpoints, and the Spreadsheet upload endpoint. Available memory, storage, and the relevant document engine are now the practical limits.
- Added a permanent workbook command bar with **Open workbook**, **Blank workbook**, **Apply to frame**, and **Download**.
- Added an original local SVG icon for **Open workbook**.
- Added dedicated **Publisher**, **Format**, and **Cells & Data** Spreadsheet ribbon tabs so frequently used commands remain directly reachable when the standard ribbon compacts.
- Added blank-workbook replacement inside an existing Spreadsheet Studio session.
- Removed the previous fixed worksheet-preview row, column, and XML-part caps. The active worksheet preview can now represent the complete imported sheet.

## Publication objects

- Added **Natural / Clip**, **Fit**, **Fill**, and **Stretch** display modes to Spreadsheet objects.
- Added the same four display modes to RichEdit text-frame objects.
- Applied those display transforms consistently to canvas rendering, page/object raster export, website export, recorded presentation output, and printing.
- Spreadsheet and text frames remain normal PublisherStudio objects: move, resize, rotate, layer, group, print, export, and double-click edit behavior is retained.

## Rotation and WordArt

- Resize pointer movement is transformed into each selected object's local rotated coordinate system.
- The opposite visual edge or corner remains anchored while a rotated object is resized.
- Resize cursors rotate with their handles and attached connectors update during the resize gesture.
- WordArt now follows the exact width and height of its publication frame instead of retaining the internal SVG aspect ratio.
- The custom freehand WordArt path editor follows the selected object's aspect ratio, so drawn geometry maps to the visible frame as expected.

## Documentation and runtime requirements

- Reworked the README opening as an offline Publisher workbench covering page layout, RichEdit documents, spreadsheets, presentations, Picture Studio, charts, barcodes, and audio/video recording.
- Documented Node.js 20+ as a source-development and release-build requirement for preparing local Spreadsheet browser assets.
- Installed/published PublisherStudio releases still do not require Node.js, npm, a CDN, or an internet connection at runtime.
- Publication format marker updated to `1.33`.

## Resource behavior

PublisherStudio intentionally imposes no application-level file upload-size limit. A very large or malformed workbook can exhaust the user's available memory or storage and may terminate the local process; this is accepted behavior for this loopback-only, user-context application.
