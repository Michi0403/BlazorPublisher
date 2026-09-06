# PublisherStudio 3.3.8 — chunked PDF release validation

## Fixed

- Fixed the PublisherStudio equivalent of the LocalGPT documentation release bug. PublisherStudio's documentation builder already emits `pdfMode: html-browser-chunked` for large DocFX sites, while its release validator rejected that valid output. `Build-Release.ps1` now recognizes the adaptive chunked browser/PDFsharp path.
- Kept the completeness gate strict for the newly accepted mode: chunked PDFs must pass the minimum source-page check and cannot report fewer source pages than generated API HTML pages.
- Aligned post-compression accessibility handling with the chunked browser mode so rewritten browser-backed PDFs retain the HTML accessibility fallback contract.
- Fixed cached browser-PDF accessibility validation: a previously validated browser-native tagged PDF can be reused from the durable cache without being misclassified merely because cache reuse is recorded as `cached-validated-pdf`; unknown accessibility states are still rejected.
- Re-verified the PublisherStudio render architecture: every routed application page remains a prerendered `InteractiveServer` page, `Error.razor` remains the intentional static fallback, and nested editor/shared components continue to inherit the owning page circuit.

## Preserved

- Package-only consumption of LocalGPT-owned `LocalGPT.ReleasePackaging` 1.0.2, adaptive bounded browser rendering, PDFsharp merge, optional qpdf/Ghostscript optimization, embedded offline help PDF, 256 MiB sane-size ceiling, Full/self-contained packaging, release resume, Apple signing/notarization, and existing application architecture remain unchanged.

## Validation

Static release/version, documentation-mode, render-boundary, source-ownership, architecture, cross-platform, async/service and XML-documentation audits are retained. This source package was not built with .NET in the editing environment; the target release hosts remain the authoritative execution test.
