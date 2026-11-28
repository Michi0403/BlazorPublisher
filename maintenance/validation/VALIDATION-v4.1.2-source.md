# PublisherStudio 4.1.2 source validation

## Scope

Source-only validation for the DevExpress runtime-coherence and scratch Panel Studio visibility repair. No .NET build, restore, publish, PowerShell build gate, GitHub or online repository access was used.

## Runtime evidence used

- The supplied 4.1.1 runtime evidence creates the blank Panel Studio document successfully, then records the DevExpress Blazor `componentContentChanged` -> `initializeComponent` null-`addEventListener` failure while the popup/control tree is still materializing.
- The 4.1.1 lifecycle snapshots show a connected Panel Studio dialog/canvas but still-pending DevExpress descendants and repeated popup/dropdown detach/reinsert activity at the failure point.
- The supplied screenshot shows newly inserted scratch elements present in the component list while the selected element is absent from the canvas and its visibility state is off.
- The supplied PublisherStudio 3.7.6 source is the user-confirmed working Panel Studio reference.

## DevExpress runtime coherence

The supplied 4.1.1 source was version-inconsistent:

- `PublisherStudio.Web.csproj`, `package.json` and `package-lock.json` requested DevExpress/DevExtreme **25.2.10**;
- the prepared `wwwroot/vendor/devextreme-dist` package metadata, `devextreme-assets.meta.json`, runtime-license metadata and runtime-key version marker still identified **25.2.10**.

PublisherStudio 4.1.2 restores the complete active lane to **25.2.10**, matching the known-good 3.7.6 source:

- `DevExpressVersion` is 25.2.10 for the Blazor/RichEdit/Spreadsheet .NET packages;
- npm and lock metadata pin `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` to 25.2.10;
- the prepared DevExtreme and Spreadsheet browser trees are byte-for-byte equal to the supplied 3.7.6 prepared trees;
- `devextreme-assets.meta.json`, `devextreme-license.meta.json` and `devextreme-license.version` all identify 25.2.10;
- active App/Spreadsheet/localization cache-busters use 25.2.10; and
- current third-party notices identify DevExpress/DevExtreme 25.2.10.

Prepared asset validation was reproduced without invoking the maintenance scripts: every asset listed in `devextreme-assets.meta.json` matches its recorded size/SHA-256 and `devextreme-license.js` matches the SHA-256 in `devextreme-license.meta.json`.

## Scratch Panel Studio visibility

- Authoritative `PublicationElement.Visible` still defaults to `true`.
- The inspector no longer writes `selected.Visible` directly from `DxCheckBox.CheckedChanged` during DevExpress editor initialization.
- Visibility is changed only by the explicit DevExpress `Show`/`Hide` action, which owns method-local diagnostics and invalidates the authoring surface.
- `AddElement` logs element ID/kind, visibility, geometry and authoring revision immediately after insertion normalization/invalidation.
- Explicit visibility changes are logged separately.
- Existing authored-render revision, first-element refresh, interaction binding and 4.1.1 browser lifecycle diagnostics are preserved.
- Panel Studio still contains 166 DevExpress controls and only the same 2 reviewed native interactive controls; no native disclosure controls were introduced.

## Maintained source audits

The available non-.NET repository audits pass after the final 4.1.2 changes:

- application architecture policy;
- async continuation policy (82 source files; 1111 await tokens);
- async-boundary component/service architecture (230 source files);
- component method resilience (3050 component methods; zero legacy exemptions);
- cross-platform boundaries (60 checks);
- Panel Studio persistence source contract;
- prerender JavaScript interop safety (3050 component methods; 13 attachment-gated JavaScript-aware disposal methods);
- Razor maintenance architecture (49 Razor components);
- service resilience (1391 service methods plus 3 iterator/yield methods);
- transient UI-state ownership (50 Razor components); and
- C#/Razor XML documentation coverage (6417 direct C# declarations, 49 Razor component types and 3853 direct Razor member declarations).

The component-attribute parser guard was reproduced against all maintained Razor components and found zero parser-fragile generic/mixed/directive expressions. `node --check` passes for `localizationRuntime.js`, `javascript-diagnostics.js` and `publisherInterop.js`. The JavaScript diagnostics inventory contains 16 maintained browser files and all normalized SHA-256 hashes match.

## Maintenance boundary

All `.ps1`, `.cmd` and `.py` maintenance/build implementations are byte-for-byte unchanged from PublisherStudio 4.1.1. The only changed file under `build/` is `javascript-diagnostics-files.sha256`, because `localizationRuntime.js` intentionally changes its DevExtreme cache-buster from 25.2.10 to 25.2.10.

## Runtime verification

A Windows runtime test remains necessary because this environment deliberately does not execute the .NET/DevExpress application. The 4.1.1 lifecycle instrumentation remains enabled, so any remaining vendor/custom-element failure will continue to expose popup/dropdown state instead of reverting to an opaque vendor stack trace.
