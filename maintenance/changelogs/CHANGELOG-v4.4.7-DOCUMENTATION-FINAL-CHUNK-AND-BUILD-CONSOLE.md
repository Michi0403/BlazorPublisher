# PublisherStudio 4.4.7 — final-chunk PDF and release-console recovery

Base: PublisherStudio 4.4.6. No architecture/build validation guards were disabled or weakened.

- **StrictMode failure on the last PDF batch:** `New-PublisherStudioHtmlPrintBook` previously assigned `@(...)` through a PowerShell `if` expression. When 753 source HTML pages are split into batches of 8, the final (95th) batch contains one file. The conditional output was unwrapped to a scalar and accessing `.Count` produced `PropertyNotFoundStrict`. The slice is now constructed within **one enclosing** `@(...)` expression, matching the already safer LocalGPT renderer.
- **Resumable recovery:** the browser rendering retry owns the active PDF candidate, removes incomplete attempts and retries with new isolated profiles within the existing configurable bound (`FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_RETRIES`, default one retry, max five). Exceptions thrown by the browser rendering step now participate in bounded retry diagnostics. Earlier validated cached chunks are not removed. Existing invalid cover/index or chunk PDFs are explicitly logged and replaced.
- **Release console consistency:** `Build-Release.ps1` now emits uniform `[Build stage: preflight]`, `[Build stage: compile]` and `[Build stage: documentation]` messages, matching LocalGPT. Both DocFX browser printers report queue and completed-chunk progress. Product-specific validation gates, package steps and documentation page counts correctly remain distinct.
- Added `tests/static/Assert-DocumentationChunkRecovery.ps1` to guard last-page array shape and recovery diagnostics. The test is separate from the production architecture gates.
- Bumped both Web and InstallerConsole to 4.4.7.

The supplied macOS log reached 94/95 PDF source batches then threw before the final batch finished. It does not prove chunk 94 or its PDF bytes were corrupt. The correct fix is the singleton handling, not forced deletion of previously validated cached PDFs.
