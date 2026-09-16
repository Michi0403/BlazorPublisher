# PublisherStudio 3.7.0 source validation

Source-only validation was performed because this environment intentionally does not provide or use the .NET SDK. No restore, build, publish, package signing/notarization, GitHub access, or GitHub API operation was run.

## Passed static checks

- Current release audit `build/audit_release_3_7_0.py`: version identity, one-digit minor/patch rule, documentation PDF-default contract, Mermaid fences/recovery, non-tiled star/glass assets, in-app/Pages asset parity, render-mode ownership, XML and JSON parseability.
- Application architecture audit: passed.
- Service resilience audit: passed; 1382 service methods own diagnostics/error boundaries and the audit reported no exemptions.
- Cross-platform boundary audit: passed (60 checks).
- Async continuation audit: passed across 80 source files.
- Component resilience audit: passed across 2687 component methods.
- Prerender JavaScript-interop safety audit: passed; JavaScript-aware disposal remains attachment-gated.
- Documentation JavaScript syntax: `node --check` passed for the authored theme and both shipped in-app copies.
- Authored documentation CSS/JavaScript bytes match the shipped help copies and the tracked Pages archive.
- Source archive integrity is checked after packaging by reopening the ZIP and hashing every entry.

## Release-specific behavior reviewed

- Documentation fallback stars are fixed-position/non-tiled; the randomized layer creates 112 desktop or 48 compact stars with independent timing, nebulae, optional planet and satellites.
- Mermaid recovery uses DocFX's bundled Mermaid module and direct `mermaid.render(...)`; there is no `offsetParent` visibility gate.
- Normal documentation builds require `PublisherStudio-3.7.0.pdf`; HTML-only output remains an explicit diagnostic opt-out through `RequirePublisherStudioDocumentationPdf=false`.
- The translucent article/navigation/card surfaces and the symmetric desktop viewport/right-rail gutters remain part of the authored and shipped documentation theme.
- Existing `InteractiveServer` ownership is unchanged; the error page remains intentionally static.
