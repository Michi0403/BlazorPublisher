# PublisherStudio 3.4.0 - release orchestration resilience

## Fixed

- Applied the LocalGPT notarization orchestration repair equivalently to PublisherStudio: recoverable Keychain/profile failures pause and retry instead of terminating the release.
- Fresh DMG/PKG notarization now uploads once, persists the Apple submission ID and artifact SHA-256 immediately, then polls the resumable state.
- `MACOS_NOTARY_WAIT_TIMEOUT` is a progress checkpoint instead of a fatal `In Progress` deadline.
- Early trust validation, per-macOS-RID validation, status polling, and notarization-log retrieval all use the same recovery rule.

## Documentation pipeline stability

- Kept the existing Edge/Chrome/Chromium adaptive chunk renderer as the production default.
- Added durable browser-PDF chunk caching under the PublisherStudio documentation cache so interrupted multi-hour runs resume from already completed chunks.
- Added per-chunk elapsed-time/size diagnostics and rendered/reused merge summaries.
- Preserved the `html-browser-chunked` validator, embedded PDF policy, and LocalGPT-owned `LocalGPT.ReleasePackaging` 1.0.2 consumption.

## Renderer/compression investigation

- WeasyPrint remains an evaluated future opt-in HTML-to-PDF backend rather than a 3.4.0 default.
- qpdf remains the preferred structural optimizer and Ghostscript the aggressive size fallback; no new mandatory native dependency was introduced.

## Additional orchestration hardening

- Transient Apple/network notarytool failures (timeouts, temporary service errors, common 408/429/5xx transport failures) now stay in a retry loop instead of terminating the coordinator; genuine terminal notarization statuses still fail explicitly.
- PowerShell native-command error promotion is locally disabled around notarytool probes so recoverable native exit codes reach the classifier instead of being converted into an early terminating error.
- Durable browser-PDF chunks now require both a valid PDF header and an end-of-file marker before reuse, so a large interrupted Edge output cannot be mistaken for a completed chunk.

## Versioning

- PublisherStudio rolled from 3.3.9 to 3.4.0 instead of creating forbidden 3.3.10.
