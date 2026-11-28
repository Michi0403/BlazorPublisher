# PublisherStudio 4.1.3 — DevExpress 25.2.10 forward-baseline repair

## Summary

PublisherStudio 4.1.3 supersedes the invalid 4.1.2 dependency downgrade. Historical working source remains useful for behavioral comparison, but an older working dependency is not a valid runtime target for a forward-moving codebase.

The maintained dependency baseline is DevExpress/DevExtreme **25.2.10** with .NET **10.0.12** package/tooling components. The 4.1.1 Panel Studio lifecycle diagnostics and the 4.1.2 explicit visibility repair remain intact.

## Fixed

- Restored `DevExpressVersion` to **25.2.10** for PublisherStudio Blazor, RichEdit and ASP.NET Core Spreadsheet packages.
- Restored npm/lock declarations for `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` to **25.2.10**.
- Restored active App, Spreadsheet and localization cache-busters to **25.2.10**.
- Updated the local `dotnet-ef` tool manifest from **an earlier .NET 10 patch** to **10.0.12**.
- Removed every source-tree reference to the superseded DevExpress patch, including stale historical retention assertions that could contradict the current dependency contract.
- Removed the bundled prepared DevExtreme/Spreadsheet payload from the superseded DevExpress patch. It is deliberately not relabeled as 25.2.10. The existing `Prepare-DevExpressAssets.cmd` / `Prepare-DevExpressAssets.ps1` contract remains the authoritative way for a licensed developer/build machine to materialize the exact 25.2.10 browser payload and runtime key.
- Preserved the explicit DevExpress Show/Hide visibility action introduced in 4.1.2 so DevExpress editor initialization cannot mutate a newly inserted element to invisible.

## Preserved

- 4.1.1 correlated C#/browser Panel Studio lifecycle diagnostics.
- 4.1.0 direct popup-body topology.
- 4.0.8 authored-render revision and 4.0.9 first-element refresh.
- Strict prepared-asset integrity verification after the exact 25.2.10 payload is prepared.
- Publication schema, Panel Library content, saved modules, behaviors, data connections and export model.

## Version

`4.1.2 -> 4.1.3`, following the maintained single-digit version-slot rule.
