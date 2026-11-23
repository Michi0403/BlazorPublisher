# PublisherStudio 4.9.4 · Ordinary Otter

The Undersuperiority Series. Includes the PDF export work from `8078a001` ("testing pdf export"). Version 4.9.3 was a version step only and was never released.

## Added — AI document creation over 1-Wire (`publisher.document.create`)
- Spreadsheet kinds: `spreadsheet` (XLSX), `xls` (Excel 97-2003 via the DevExpress spreadsheet engine) and `csv` (the normalized CSV, UTF-8). The publication always gets the native XLSX spreadsheet object.
- New kinds besides `spreadsheet`, `text` (Word DOCX) and `pdf`: `rtf`, `odt` (OpenDocument), `html` (images embedded), `txt` (UTF-8, exactly the requested text), `epub`, `doc` (Word 97-2003) and `mht` (single-file web archive).
- `RichTextDocumentFactory.Export` renders the same Open XML story with the server-side RichEdit engine; the text object inserted into the publication is unchanged.
- Function catalog: display name, kind enum and input contract updated. Pairs with LocalGPT 7.7.8, which validates and serves the new formats.

## Checks actually performed
- DevExpress 25.2 RichEdit core checked for the used members (`DocumentFormat` Rtf/OpenDocument/Html, `EmbedImages`, EPUB exporter present). `DocumentFormat.ePub` member name not confirmed from metadata.
- **No .NET build or 1-Wire test has been run.**
