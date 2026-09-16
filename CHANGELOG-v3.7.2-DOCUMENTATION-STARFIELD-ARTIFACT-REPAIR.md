# PublisherStudio 3.7.2 — documentation starfield artifact repair

## Changed

- Remove the remaining documentation pseudo-nebula layer and disable `backdrop-filter` on documentation panels. The glass appearance is retained with translucent surfaces, borders and shadows, avoiding the tiled Chromium compositor path visible as purple squares over the star field.
- Keep the randomized star sky viewport-contained and animated. Per-star brightness filters are removed so twinkle uses opacity rather than creating extra filter layers.
- Keep satellites guaranteed on every page, increase their scale/contrast, shorten their travel periods and give the two desktop satellites long opposing side-gutter flight paths.
- Preserve the attached-node Mermaid renderer, bounded Mermaid recovery, diagrams, right-side gutter, documentation navigation and required PDF source contract from 3.7.1.

## Visual/front-end release checklist

- Documentation visual ownership remains inside the existing themed DocFX shell; no new application-wide stacking root is introduced.
- The sky remains `position: fixed`, viewport-contained and pointer-transparent; it does not participate in publication/Mainframe object layering or input handling.
- GPU backdrop filtering is disabled for the documentation shell; translucent cards provide the glass appearance without a second gesture/input layer.
- JavaScript sky creation remains idempotent and has no persistent observers/listeners requiring new disposal behavior.
- Print mode continues to hide the decorative sky.
- No PublisherStudio editor, media, export, object-layer or canonical publication behavior changes in this release.

## Validation boundary

This repository copy is validated without `dotnet` in this environment. Static architecture, resilience, cross-platform, async/component/prerender, documentation parity, JavaScript syntax, JSON/XML, render-mode and archive-integrity checks are recorded in `VALIDATION-v3.7.2-source.md`. No GitHub or GitHub API access is used.
