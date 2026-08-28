# PublisherStudio 3.0.8 — macOS Documentation PDF Runtime Repair

## Fixed

- Added the missing standard macOS application-bundle probes for Google Chrome, Microsoft Edge, and Chromium to the existing documentation browser resolver.
- Keeps the preferred single-browser complete-PDF path available on macOS instead of falling through solely because GUI browsers are normally absent from `PATH`.
- Streams DocFX fallback output while it runs so a long PDF operation does not appear frozen.
- Suppresses only DocFX's redirected in-place `Removed ... files` / `Copied ... files` transfer-counter redraws, whose carriage-return formatting becomes internally inconsistent after PowerShell redirection; raw records remain captured for diagnostics.
- Keeps documentation generation mandatory, keeps the strict HTML accessibility/link preflight unchanged, and keeps the PDF requirement unchanged.
- No application, UI, InteractiveServer, service, deployment, DevExpress, device, persistence, or publication-model behavior was changed.

## Version

- PublisherStudio web application and installer console: `3.0.8`.
- Browser asset/cache identity and npm package identity: `3.0.8`.
- LocalGPT wire protocol remains `2.1.1`.
- Minor and patch version slots remain single-digit.
