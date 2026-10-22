# PublisherStudio 4.3.9 — native AI assistance and browser-view awareness

## Changed

- `AI Assist selected object` is visible in the page context menu only while LocalGPT advertises a linked Council capability. Ribbon actions are also gated.
- The Story and Spreadsheet choices are routed back to the native PublisherStudio editor by EventCallback rather than navigating to `/organic-plugins?assist=...` and opening connection setup.
- Story Editor correlates each new AI request with a single unique proposal target; while the originating popup is open, the matching final proposal inserts directly into DevExpress RichEdit at the caret. Other proposals remain reviewable.
- Spreadsheet Studio now contains a native LocalGPT request panel using the current linked Council skills. It registers a single-document insertion intent before asking the model for CSV, then uses the existing editor history and normal spreadsheet-document operations to incorporate a matching approved proposal. The AI-originated workbook session is reloaded when the result arrives.
- Added `publisher.browser.view.inspect`, an explicitly permissioned, read-only 1-Wire capability backed by the existing automation queue. When the human has enabled the PublisherStudio browser automation tab, `InspectView` returns bounded visible viewport controls, labels, publication page geometry, selected element IDs and bounding rectangles. Form-field contents, password input values and hidden elements are omitted. The response is retrieved via `publisher.input.result` by request ID. No new remotely callable unauthenticated endpoint was introduced.
- Added the new capability to the resettable DXFunction seed catalog and a Node regression test.
- Updated browser cache query for `automationInterop.js` and refreshed its exact SHA-256 diagnostics inventory. Both projects are versioned 4.3.9.

## Explicit limitations

- The present 1-Wire Council bridge reports a completed proposal, **not token deltas**. The editor automatically inserts the completed result; genuine live-by-token typing into DevExpress RichEdit and individual spreadsheet cells requires a further authenticated progress-event protocol. This release does not claim that streaming exists.
- Browser view inspection represents the **current visible browser tab**, not unseen publication pages or offscreen content, and requires operator opt-in. `publisher.input.execute` remains the existing approved browser-command path. A complete task-planned, cross-page, media-aware publication generator and generic Div Studio semantic command interface are not implemented by these changes.
- Ollama transport lifetime is addressed in companion LocalGPT 6.2.0. PublisherStudio does not own that transport.
- Real Windows / DevExpress / .NET compilation, live paired Council runs and actual media editing have not been tested in this environment. See `VALIDATION-v4.3.9-source.md`.

## Maintenance

All pre-existing PowerShell architecture checks remain unchanged; no Python maintenance tests or `.NET` build attempts were added. The existing Picture Studio/GIF code is unchanged.
