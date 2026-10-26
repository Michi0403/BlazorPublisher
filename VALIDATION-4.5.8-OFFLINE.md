# PublisherStudio 4.5.8 — offline validation

- Maintained `.cjs` source regression suite passed (including newly added Mermaid/touch checks).
- Maintained documentation JavaScript syntax passed `node --check`.
- Portable Node test `tests/js/documentation-mermaid-touch-behavior-458.test.cjs` passed: measured oversized SVG label, fitted already-rendered label, mobile Scratch/Scroll toggle.
- No .NET compilation, DocFX regeneration or physical browser/mobile acceptance test was performed. Chromium navigation was restricted by the execution environment, so full rendering has not been visually certified.
