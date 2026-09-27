# PublisherStudio 3.8.3 — Windows PowerShell 5.1 maintenance-guard repair

## Fixed

- Fixed the German identical-text baseline loader so `App.Name = "PublisherStudio"` remains a reviewed language-neutral product name instead of being falsely rejected under Windows PowerShell 5.1.
- Fixed `Assert-RazorComponentAttributeExpressions.ps1` for one-root repositories by outer-materializing the root-selection pipeline before `.Count`.
- Hardened publish-profile output discovery against the same scalar result behavior.
- Extended the PowerShell compatibility guard to reject array-wrapped `ConvertFrom-Json` and unmaterialized filtered array pipelines that later rely on `.Count`, with file/line diagnostics and architectural repair choices.
- Added the PowerShell maintenance-guard contract to `AGENTS.md`.

## Preserved

- DevExpress component-retention enforcement stays build-breaking.
- DataVisual editor enum lists stay precomputed typed properties, avoiding `RZ9986` without replacing DevExpress controls.
- DevExtreme and DevExpress ASP.NET browser packages remain `25.2.10`; the cross-platform asset helper path is unchanged.
- InteractiveServer/prerender boundaries and the JavaScript diagnostics integrity contract remain unchanged.

## Version

- PublisherStudio Web: `3.8.2` → `3.8.3`.
- PublisherStudio installer: `3.8.2` → `3.8.3`.
- npm package root: `3.8.2` → `3.8.3`.
