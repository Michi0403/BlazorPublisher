# PublisherStudio 4.1.0 source validation

## Scope

Source-only validation for the repeated Panel Studio DevExpress lifecycle failure. No .NET build, restore, publish, PowerShell build gate, GitHub or online repository access was used.

## Runtime evidence used

- The supplied Windows run creates `Panel Studio preset blank with 1 views` and then reports the DevExpress Blazor `Cannot read properties of null (reading 'addEventListener')` failure in `initializeComponent` / `componentContentChanged`.
- Panel Studio subsequently reports the interaction binding with `PreviewRevision:0 AuthoringRevision:0`, so the initialization failure begins before authored-element mutation or authored-render revision changes.
- The supplied PublisherStudio 3.7.6 source remains the known-good comparison for scratch Panel Studio authoring.

## Panel Studio topology repair

- Compared `Components/Editor/PanelStudio.razor` in supplied 3.7.6 with 4.0.9.
- The 3.7.6 popup body has no nested maintenance `DxFormLayout` shells; 4.0.9 still had eleven of them around palette, view settings and conditional inspector sections.
- Removed those eleven shells while retaining their real editor content and current DevExpress controls.
- Added one component-level FormLayout owner outside the popup body, matching the maintained editor architecture.
- Verified the `DxPopup.BodyContentTemplate` still exposes `.panel-studio-dialog` directly.
- No JavaScript exception suppression, native dialog fallback or arbitrary delay was added.

## Preserved regression repairs

- 4.0.8 authored-render revision and readable Panel Studio labels remain.
- 4.0.9 empty-to-first-element refresh remains.
- The exact prepared DevExtreme stylesheet restored in 4.0.9 remains unchanged and the export hash gate remains strict.

## Source audits

- Razor maintenance architecture passes with a single component-level Panel Studio FormLayout and no nested maintenance section layouts in the popup body.
- Additional maintained source audits are recorded in `VALIDATION.md` after the final package validation.
- Build/maintenance scripts remain byte-for-byte unchanged from 4.0.9. The only `build/` data change is `devexpress-component-retention.json`, where Panel Studio's protected minimum moves from 186 to 166 to account exactly for the twenty removed `DxFormLayout`/`DxFormLayoutItem` shell tags; native interactive/disclosure maxima remain unchanged.

## Release identity

- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.1.0**.
- Version progression follows the maintained convention: `4.0.9 -> 4.1.0`.

## Completed source checks

- Application architecture audit passed.
- Async continuation audit passed for 82 source files.
- Async-only component/service architecture audit passed for 230 PublisherStudio source files.
- Component method resilience audit passed for 3048 component methods.
- Prerender JavaScript interop safety audit passed for 3048 component methods.
- Razor maintenance architecture audit passed for 49 Razor components.
- Service resilience audit passed for 1391 service methods plus the maintained iterator set.
- Transient UI-state ownership audit passed for 50 Razor components.
- Panel Studio persistence/source lifecycle audit passed.
- Cross-platform boundary audit passed.
- XML documentation coverage passed for 6417 direct C# declarations and 3848 direct Razor `@code` members.
- The text-service ownership rule was reproduced against its unchanged baseline and has no new direct string/regex findings.
- The DevExpress retention rule was reproduced against the updated architectural contract: Panel Studio retains 166 DevExpress tags, 2 reviewed browser-owned native interactive tags and 0 native disclosure tags.
- Every prepared DevExtreme asset listed in `devextreme-assets.meta.json` matches its recorded byte count and SHA-256.
