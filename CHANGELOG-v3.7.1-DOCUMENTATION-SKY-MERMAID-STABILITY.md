# PublisherStudio 3.7.1 — documentation sky and Mermaid stability

## Changed

- Keep the randomized deep-space sky viewport-contained so decoration cannot increase document dimensions.
- Retain 112 independently positioned desktop stars with more visible independent drift and guaranteed side-gutter satellites; the non-tiled CSS fallback disappears after the dynamic sky mounts.
- Remove large `filter: blur(...)` nebula DOM layers that could rasterize as purple square compositor tiles. Nebula depth now uses compositor-safe radial gradients while documentation panels keep a moderate glass blur.
- Render Mermaid through attached DOM nodes with SVG labels and bounded recovery instead of detached `mermaid.render()` retries.
- Emit the maintained architecture flowcharts as Mermaid HTML blocks rather than syntax-highlighted `lang-mermaid` code, eliminating the Highlight.js missing-language warning.
- Preserve the documentation gutter, glass hierarchy, PDF-required source contract and established InteractiveServer render-mode ownership.

## Validation boundary

This is a source-only release preparation. Static architecture, resilience, cross-platform, async/component/prerender, documentation parity, JavaScript syntax, JSON/XML, render-mode and archive-integrity checks are recorded in `VALIDATION-v3.7.1-source.md`. No .NET build/restore/publish was run and no GitHub/GitHub API access was used.
