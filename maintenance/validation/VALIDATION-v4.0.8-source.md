# PublisherStudio 4.0.8 source validation

## Scope

Source-only validation for the Panel Studio scratch-authoring/lifecycle repair. No .NET build, restore, publish, PowerShell build gate, GitHub or online repository access was used.

## Release identity

- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.0.8**.
- Version progression follows the maintained single-digit minor/patch convention: `4.0.7 -> 4.0.8`.

## Panel Studio regression review

- PublisherStudio 3.7.6 and 4.0.7 source trees were compared directly for `PanelStudio`, `PanelView`, panel document normalization, Panel Studio interaction JavaScript and related CSS.
- The underlying panel document model and `PanelView` element renderer are materially continuous across the known-good and failing versions; the later frontend lifecycle/DevExpress migration is the relevant regression boundary.
- `PanelStudio` now advances an explicit authored-render revision after maintained mutable graph changes and passes that primitive revision to `PanelView`.
- Existing keyed live element instances are preserved; the repair does not remount the entire panel on every insertion.
- The DxPopup attachment fallback polls for the materialized surface without forcing intermediate Blazor rerenders.
- Panel Studio text-only list selectors override the generic Visual editor icon-column layout and no longer use arbitrary character wrapping.

## Preservation checks

- `build/` is unchanged from PublisherStudio 4.0.7.
- Publication persistence models and schemas are unchanged.
- DevExpress controls are retained; no native editor/action substitution was introduced.
- The 4.0.6 text-service ownership and 4.0.7 compiler fixes remain present.

## Tooling note

This environment intentionally did not execute `dotnet`, MSBuild, restore or publish. Runtime/browser confirmation must therefore come from the normal PublisherStudio Windows build/run cycle.
