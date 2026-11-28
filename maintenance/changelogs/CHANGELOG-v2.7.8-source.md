# PublisherStudio 2.7.8 source changes

- Fixes the initial text-frame StoryEditor mismatch: the default `Your publication` preview now receives a real editable OpenXML source at creation time instead of an empty `DocumentContent` payload.
- Adds a centralized `RichTextDocumentFactory.CreateOpenXmlFromPreviewHtml(...)` recovery path so preview-backed text frames created by templates, panels, imports, or older publications can be opened in RichEdit without losing the text already visible on the canvas.
- Repairs publication deserialization for legacy/incomplete text frames: an absent rich-text payload is reconstructed from that frame's own sanitized `PreviewHtml`; the unrelated hard-coded `Text frame` fallback is removed.
- Adds the same recovery guard at StoryEditor load time for in-memory elements that have not gone through file deserialization yet.
- Preserves user edits as authoritative: once RichEdit has saved an OpenXML package, preview recovery is not used, preventing old/default preview copy from being reintroduced over saved content.
- Reviewed PublisherStudio's LocalGPT/1-Wire bridge against the supplied LocalGPT 3.0.5 source. Existing live Council, business-context, text-proposal, reviewed-text, spreadsheet, media, screen, and related capability paths remain intact. LocalGPT's embedded-wiring capability is explicitly described there as future-facing, so PublisherStudio does not falsely advertise an unimplemented embedded workbench endpoint in this maintenance release.
- Preserves the reviewed InteractiveServer render boundaries exactly; no competing child render modes were added or removed.
- Bumps PublisherStudio Web and InstallerConsole to `2.7.8` and refreshes active PublisherStudio JavaScript module cache-busters to `2.7.8`.
- Wire protocol remains `2.1.1`.
