# Stories and spreadsheets

PublisherStudio embeds rich documents and workbooks as normal publication objects.

## Rich stories

Story editing uses DevExpress RichEdit. A story can keep formatted text, page settings, fields, backgrounds, and document content inside the publication.

Apply saves the edited story back to the selected text frame. Cancel leaves the existing object unchanged.

## Spreadsheets

Spreadsheet Studio uses the DevExpress ASP.NET Core Spreadsheet in a same-origin editor surface. Workbooks can be created or imported, edited, downloaded, and applied back to a spreadsheet frame.

The page canvas and exports use a safe preview of the workbook. Executable workbook content is not injected into the publication DOM.

## Practical tips

- Keep source workbooks when macros or unsupported features matter.
- Use **Fit** when a whole sheet should remain visible.
- Use **Clip** when the frame is a deliberate viewport.
- Reopen a spreadsheet frame to continue editing the embedded workbook.
