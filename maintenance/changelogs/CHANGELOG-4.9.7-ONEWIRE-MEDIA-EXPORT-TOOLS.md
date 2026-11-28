# PublisherStudio 4.9.7 · Tolerable Tapir

The Undersuperiority Series. Pairs with LocalGPT 7.8.1 · Adequate Armadillo.
Still not the Squirrel release; the Nuts engine is not here yet either.

## Since 4.9.5
- **4.9.6 · Ordinary Otter:** `publisher.document.create` advertises and returns PDF again, rendered from the story through `publisher.renderStoryPdf` in the embedded export bridge (the advertised kinds are spreadsheet, text, pdf, rtf, html, txt and csv).
- **4.9.7 · Tolerable Tapir:** see below.

## Added: media and export tools over 1-Wire (`publisher-dx-functions.json`)
- `publisher.export.formats`: one discovery call for document kinds, publication/website scopes and formats, the render export catalog, FFmpeg presets and profiles, import families and the 1-Wire file budget. Every list comes from its owning catalog or service.
- `publisher.media.convert`, `publisher.media.job`, `publisher.media.output`: FFmpeg conversion of a peer file (preset or operator profile), progress, and the converted file. `publisher.media.convert` is the key LocalGPT's add-on manifest already referenced.
- `publisher.publication.export`: page/object (PNG, JPG, SVG), all pages (PNG/JPG, ZIP when several), single-file presentation or site (HTML), structured website (ZIP), through the editor's own export path.
- `publisher.file.read`: files above one 1-Wire message are announced with length and SHA-256 and read in chunks; only the requesting peer can read them.
- UI exports and 1-Wire exports share the same JS producer functions.

## Settings (`OrganicPlugins:MediaTools`)
`MaximumListedJobs`, `EditorExportTimeoutSeconds`, `DefaultExportDpi`, `AllowPeerFilterAndAdvancedArguments` (default false), `MaximumTransferFileBytes`, `MaximumRetainedTransferBytes`, `TransferRetentionMinutes`.

## Fixed
- Single-page export looked for `.print-publication > .print-page` (never matched) and always rasterized the on-screen editor page.
- Story export used a fixed 30 s timeout; `publisher.media.jobs` a fixed 50. Both are settings now.
- AI Assist said "a live session still needs to be opened" while linked.
- Documentation PDF: Windows hosts with 12-16 GB print 32-page parts.

## Checks actually performed
VS Debug build with all repository PowerShell gates; JS unit tests touching the changed files; on screen: pairing, link, Ping/Pong, capability listing, all export producers. The LocalGPT -> PublisherStudio end-to-end export/convert and chunked transfer were **not** tested yet.
