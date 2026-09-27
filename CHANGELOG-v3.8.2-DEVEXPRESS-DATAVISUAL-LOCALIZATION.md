# PublisherStudio 3.8.2 — DataVisual Razor compile and maintenance-architecture repair

## Fixed

- Repaired the reported `RZ9986` failures in `DataVisualEditor.razor`. The seven `Enum.GetValues<T>()` expressions are now prepared as typed component properties and the DevExpress `DxComboBox` controls bind those simple members.
- Kept the existing DevExpress controls; the fix does not replace them with HTML controls.
- Added a pre-compile Razor component-attribute guard for direct generic expressions and mixed literal/C# component attributes. Findings include exact file/line/column information and explain the valid architectural choices, including typed render-state properties and `async () => await MethodAsync(context)`/named async handlers for Task-returning event callbacks.
- Added the Blazor multi-render-state lifecycle model to `AGENTS.md`, covering prerender/static SSR, InteractiveServer, InteractiveWebAssembly and inherited child-component execution as separate instance/entry paths.
- Canonicalized Data Visual localization source casing so case-insensitive duplicate keys such as `Text.Polar chart` / `Text.Polar Chart` no longer coexist. The canonical Title Case keys are used consistently by UI callers and persisted default visual titles.
- Added German translations for the Data Visual validation/status strings and the recording-finalization status that were previously wrapped for localization but absent from the catalogs.
- Localization validation now reports source/catalog lines plus architectural choices, checks exact key parity in all maintained cultures, rejects case-insensitive duplicate keys and verifies every literal `LT("...")` / `GetText("...")` call exists in `en-US.json`.
- DevExpress retention diagnostics now include source locations and the accepted architecture rather than suggesting control removal as an easy path.
- Central architecture-audit failures now emit MSBuild-style file/line diagnostics and remediation choices.

## Preserved from 3.8.1

- DevExtreme / DevExpress browser assets remain pinned to `25.2.10`.
- `Prepare-DevExpressAssets.ps1` remains Windows PowerShell 5.1 compatible by using the checked-in Node helper to parse npm v3 `package-lock.json`; no fragile `node -e` JavaScript quoting is used.
- Existing browser assets are kept until a replacement asset set is prepared successfully.
- InteractiveServer page/island render boundaries and prerender behavior are unchanged.

## Version

- PublisherStudio Web: `3.8.1` → `3.8.2`.
- PublisherStudio installer: `3.8.1` → `3.8.2`.
- npm package root: `3.8.1` → `3.8.2`.
