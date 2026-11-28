# PublisherStudio 4.2.8 — source validation

## Reproduction and assertions

- Direct evidence: PublisherStudio 4.2.7 Debug log reports `Browser PDF path preflight failed on Windows (output=258 chars, profile cache=153 chars)` for the first `front-matter.pdf`; no PDF process was started.
- Reconstructed the exact logged Windows path and the new repository-owned, hash-keyed PDF-part path. Before: 258 characters. After: 192 characters. The original browser guard remains `output <= 240` and `profile cache <= 170`.
- Verified the PDF output/cache path is now based on the existing `Initialize-Future2RepositoryBuildStorage` result (`$buildStorage.Documentation`), which respects explicit documentation-cache and build-storage overrides without changing the HTML durable cache.
- Verified that the cache key is the existing unmodified full documentation content hash, the `v2-p<n>-n<n>` chunk scope remains, and that the PDF integrity checks and managed `pdf-merge --input-list` merge flow are unchanged.
- Verified continued five-second printing heartbeats and completed-part progress reporting from 4.2.7; no forced fallback, disabled assertions, or altered PDF thresholds.
- Verified only the documentation script, version references and the two release-note files differ from PublisherStudio 4.2.7.
- Verified archive contents and ZIP CRC against the complete 4.2.7 source baseline.

## Runtime limits

Not a Windows PowerShell 5.1 / .NET / Microsoft Edge build or runtime test. This release fixes the exact detected path-preflight failure. Browser PDF printing, merger execution, and final PDF generation still require verification by the user's Windows Debug build.
