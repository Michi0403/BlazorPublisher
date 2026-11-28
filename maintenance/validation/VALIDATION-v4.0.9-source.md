# PublisherStudio 4.0.9 source validation

## Scope

Source-only validation for the Panel Studio DevExpress popup lifecycle and website export prepared-asset repair. No .NET build, restore, publish, PowerShell build gate, GitHub or online repository access was used for the package validation.

## Release identity

- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.0.9**.
- Version progression follows the maintained single-digit minor/patch convention: `4.0.8 -> 4.0.9`.

## Panel Studio regression review

- Compared the current Panel Studio structure with the supplied known-good 3.7.6 source.
- Removed only the maintenance FormLayout that wrapped the portaled `DxPopup`; `DxPopup` remains the actual modal owner and the real `.panel-studio-dialog` remains its direct body surface.
- Moved the corresponding FormLayout ownership onto the local left-side editor group, preserving the maintained DevExpress component count and avoiding native-control fallback.
- The first element inserted into an empty scratch view increments `_previewRevision` once, remounting the authored `PanelView` only for the empty→populated transition.
- Existing populated/library panels and subsequent changes continue to use the authored-render revision without repeatedly remounting live content.

## DevExtreme export integrity

- `vendor/devextreme-assets.meta.json` declares `devextreme-dist/css/dx.light.css` as 691921 bytes with SHA-256 `e4c55c72f636bf286bdf41cc4ced7e48c3f2ea37172973dc5a845b34b607bc37`.
- The supplied 4.0.8 stylesheet was also 691921 bytes but hashed as `7f059dc33034a6a417add2f3d3417be1aeb89fa987b18976164c0c366942f2b2`. The mismatch was real, not a browser-cache false positive.
- The mismatch consisted of nine single-byte changes inside embedded SVG generator metadata. The prepared stylesheet was restored from the supplied 3.7.6 tree, which uses the same prepared DevExtreme 25.2.10 asset and matches the current manifest exactly.
- Every asset listed in `devextreme-assets.meta.json` was re-hashed after the repair; zero mismatches remain. The runtime/export integrity check itself was not weakened.

## Source audits

- Razor maintenance architecture passed after moving FormLayout ownership away from the popup portal.
- Transient UI-state ownership passed.
- Panel Studio persistence/source lifecycle audit passed.
- DevExpress/native-control static counts for `PanelStudio.razor` remain at the protected level: 186 DevExpress tags and the same two reviewed browser-owned native buttons.
- `build/` remains byte-for-byte unchanged from 4.0.8.

## Tooling note

This environment intentionally did not execute `dotnet`, MSBuild, restore, publish or PowerShell. Runtime confirmation of the DevExpress browser behavior remains the user's normal Windows/DevExpress build/run step.
