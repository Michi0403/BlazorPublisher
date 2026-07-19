# PublisherStudio v1.0.32 — Open workbook inside Spreadsheet Studio

## Added

- Added an **Open workbook** command directly to the DevExpress Spreadsheet **Home** ribbon.
- The command opens a local file picker without leaving the Spreadsheet creation/editing modal.
- Supported formats are XLSX, XLSM, XLS, CSV, TXT, and TSV.
- The selected workbook replaces the current workbook in the active Spreadsheet Studio session and reloads the control with a new DevExpress document identifier.
- The modal title updates to the loaded workbook name after the replacement control is ready.
- Existing Spreadsheet drag-and-drop loading remains unchanged and continues to work.

## Workflow and safety

- Unsaved spreadsheet edits trigger a confirmation before another workbook is opened.
- The toolbar import uses the existing same-origin ASP.NET Core server and anti-forgery token; no cloud upload or external service is involved.
- Workbook type/signature validation is reused from `SpreadsheetDocumentService` before session content is replaced.
- Uploads are limited to 64 MB on both the browser and server sides.
- Apply, Download, Cancel, and repeated Open commands are blocked while a replacement workbook is loading, preventing session races.
- A visible loading overlay and 25-second timeout cover the upload/reload cycle; failures leave the existing editor usable and show the server error.
- Opening a workbook creates a fresh DevExpress document ID so server-side document caching cannot return the workbook that was previously open in the session.

## Runtime requirements

Node.js is still required only by developers and release-build machines when `Prepare-SpreadsheetAssets.cmd` prepares the offline DevExpress browser assets. End users do not need Node.js or npm; toolbar imports run entirely inside the published ASP.NET Core application.

## Validation

- Confirmed the custom ribbon item uses the supported Spreadsheet ribbon builder and `OnCustomCommandExecuted` event path.
- Confirmed the multipart upload is anti-forgery protected and session-bound.
- Confirmed replacement workbook metadata, preview HTML, active worksheet, storage format, filename, and document ID are updated atomically in the session store.
- Confirmed JavaScript syntax, C# syntax trees, Razor code blocks, JSON, project XML, and source archive integrity.
- A complete licensed DevExpress runtime build and browser interaction test must still be run on the licensed development machine because this environment does not contain the .NET SDK or DevExpress package feed.
