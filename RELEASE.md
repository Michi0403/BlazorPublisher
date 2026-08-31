# PublisherStudio 3.2.1

PublisherStudio 3.2.1 hardens the **macOS documentation/PDF release path** while preserving the optional headless WSL2 Linux backend and native Windows/Linux/macOS release lanes from 3.2.0.

A real PublisherStudio 3.1.9 macOS run reached 83% of the DocFX PDF render, reported page-load timeouts, then hit the 1,800,000 ms Playwright timeout and failed after Edge also reported `Printing failed`. The release path now prefers a validated browser-generated complete handbook and retries with a lower-overhead compatibility renderer before invoking the DocFX Playwright PDF plug-in.

Compatibility PDFs are explicitly recorded as `html-browser-print-compatibility` with `html-accessibility-fallback`; generated HTML accessibility and local-link preflight remains strict. The DocFX plug-in stays available as a last fallback, but macOS defaults to a five-minute fallback timeout instead of an unavoidable thirty-minute floor. A positive operator-supplied `DOCFX_PDF_TIMEOUT` is honored exactly.

WSL2 delegation, parent-prepared DevExpress assets/documentation, LocalGPT-owned local-first packaging, Windows installers, Linux packages, and explicit InteractiveServer boundaries remain unchanged.

See `CHANGELOG-v3.2.1-MACOS-DOCUMENTATION-PDF-RENDER-RECOVERY.md`, `VALIDATION-v3.2.1-source.md`, and `docs/articles/wsl-linux-release.md`.
