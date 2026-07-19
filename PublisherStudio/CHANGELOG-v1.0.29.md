# PublisherStudio v1.0.29 — Spreadsheet Studio

Release date: 2026-07-19

## Added

- Added `SpreadsheetElement` as a first-class publication object with the same selection, transform, Z-order, visibility, lock, grouping, clipboard, animation, context-menu, inspector, thumbnail, print, and export participation as other canvas objects.
- Added **Insert > Spreadsheet** for XLSX, XLSM, XLS, CSV, TXT, and TSV files.
- Added **Create spreadsheet** for a new embedded XLSX workbook.
- Added double-click editing and a contextual **Spreadsheet Tools** ribbon with Edit, Download, Open, and Blank commands.
- Added a full DevExpress ASP.NET Core Spreadsheet editor inside an application-styled PublisherStudio modal.
- Added Apply, Download workbook, and Cancel behavior matching the existing Story, Picture, Audio, and Video Studio workflows.
- Added workbook bytes, file name, storage format, active worksheet, grid display, background, border, and safe static preview data to the publication model.
- Added static worksheet rendering for OpenXML workbooks and delimited text so spreadsheet frames appear in the canvas, page thumbnails, print surface, image/SVG export, animated HTML, and recorded presentation output.

## Integration architecture

- The DevExpress Spreadsheet is an ASP.NET Core MVC/Razor control rather than a native Blazor component. It is integrated as a same-origin MVC/Razor island in an iframe while the surrounding dialog, lifecycle, and publication state remain Blazor.
- Added `SpreadsheetController`, `SpreadsheetSessionStore`, `SpreadsheetDocumentService`, and a restricted `postMessage` bridge.
- Added unique DevExpress document IDs per editor session, four-hour abandoned-session cleanup, and DevExpress document hibernation under the user's local application-data directory.
- Added local DevExpress Spreadsheet, DevExtreme, and jQuery asset preparation. Published builds serve all assets from PublisherStudio's loopback ASP.NET Core host and require no CDN or internet access at runtime.

## Document handling

- Blank spreadsheets are created as valid minimal XLSX packages.
- XLSX and XLSM are opened directly; XLS, CSV, TXT, and TSV can be imported through the DevExpress editor.
- Edited XLS, CSV, TXT, and TSV documents are saved back as XLSX. XLSM stays macro-enabled when saved.
- Workbook downloads preserve the current embedded workbook and normalized file extension.
- Active-cell editing is committed before custom save, and save waits for Spreadsheet synchronization to finish before retrieving the client state.

## Security and stability

- Added anti-forgery headers to every DevExpress Spreadsheet internal request and to custom save requests.
- Added server-side anti-forgery validation on the Spreadsheet controller.
- Added document-ID verification so a submitted client state cannot be applied to a different PublisherStudio spreadsheet session.
- Added XLSX/XLSM package validation and legacy XLS compound-file signature validation before opening a workbook.
- Spreadsheet preview HTML is regenerated from the embedded workbook when publications are loaded; stored preview markup is never trusted.
- Static previews HTML-encode cell contents and emit only normalized, whitelist-generated styles.
- Added a 25-second editor startup watchdog and visible initialization/save errors instead of leaving the modal permanently on “Opening workbook”.

## Build and deployment

- Added `DevExpress.AspNetCore.Spreadsheet` 25.2.8.
- Added exact npm dependencies for `devexpress-aspnetcore-spreadsheet`, `devextreme-dist`, and `jquery`.
- Added a client-asset preparation script and MSBuild/release-build checks.
- Updated source-package exclusions so `node_modules` and generated proprietary DevExpress browser assets are not redistributed.
- Publication format marker updated to `1.29`.

## Validation performed in this environment

- C# syntax-tree parsing completed without syntax errors.
- JavaScript and Node asset-preparation scripts passed `node --check`.
- Razor component code blocks, JSON files, project XML, and archive integrity were checked.
- Workbook validation, preview parsing, editor-session binding, and export wiring were reviewed through source-level static inspection.

A full `dotnet restore`/`dotnet build` and licensed DevExpress runtime test could not be run in this environment because the .NET SDK and the licensed DevExpress package feeds are unavailable. The npm restore also could not be completed here, so generated proprietary client assets are intentionally absent from the source ZIP and must be restored on the licensed build machine.
