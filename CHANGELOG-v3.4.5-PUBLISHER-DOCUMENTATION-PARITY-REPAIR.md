# PublisherStudio 3.4.5 — documentation parity repair

## Fixed

- Restored the missing `$localApplicationData` initialization in `build/Build-Documentation.ps1`. Under `Set-StrictMode -Version Latest`, PublisherStudio 3.4.4 referenced this variable before it was defined, which prevented browser discovery from reaching the macOS `/Applications` Edge/Chrome probes and caused an unintended fallback to the DocFX PDF plug-in.
- Large documentation sets now require the adaptive browser-chunk pipeline when the managed release-packaging helper is available. If browser rendering cannot start or complete, the release fails immediately with the browser diagnostic instead of spending tens of minutes generating a multi-gigabyte DocFX fallback PDF.
- Preserved the durable chunk cache and PDFsharp merge path used by LocalGPT: completed chunks are reusable and only missing chunks are rendered.
- Preserved PublisherStudio's HTML/API/accessibility/link validation and accepted `html-browser-chunked` as the release PDF mode.
- Extended artifact-local notarization state to distinguish the SHA-256 submitted to Apple from the final stapled artifact SHA-256. Stapling is an expected byte mutation and no longer invalidates a completed artifact's own state record.
- Kept one-shot per-artifact `notarytool submit`, history reconciliation for ambiguous local results, idempotent `info` polling, and independent DMG/PKG state.

## Release policy

PublisherStudio 3.4.5 keeps the existing architecture and InteractiveServer render-mode ownership unchanged. No application feature behavior was intentionally changed by this repair.
