# PublisherStudio 4.1.3 source validation

## Scope

Source-only correction of the invalid 4.1.2 dependency rollback. No .NET build, restore, publish, GitHub repository access or DevExpress vendor payload fabrication was performed.

## Dependency baseline

- `PublisherStudio.Web.csproj` uses `DevExpressVersion` **25.2.10**.
- `package.json` and `package-lock.json` pin `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` to **25.2.10**.
- Active App/Spreadsheet/localization cache-busters use **25.2.10**.
- The local `dotnet-ef` manifest is **10.0.12** and maintained Microsoft/System 10.0 package references remain **10.0.12**.
- Repository text/code contains zero superseded DevExpress-version occurrences.

## Prepared browser assets

The supplied source history only contained prepared DevExtreme/Spreadsheet payloads identifying themselves as an older superseded DevExpress patch. 4.1.3 removes those stale generated browser payloads rather than changing their labels or hashes. The unchanged preparation pipeline remains responsible for restoring/generating the exact licensed 25.2.10 payload and metadata before browser runtime/export validation.

## Panel Studio

- 4.1.1 correlated lifecycle diagnostics are preserved.
- The 4.1.2 explicit Show/Hide visibility action is preserved.
- New publication elements continue to default visible; no direct DevExpress checkbox initialization callback owns authored visibility.

## Source checks performed

- zero superseded DevExpress-version occurrences across PublisherStudio source;
- active DevExpress declarations/cache-busters are 25.2.10;
- active .NET tool/package patch declarations reviewed are 10.0.12;
- JavaScript diagnostics manifest was refreshed for the intentional localization runtime cache-buster change;
- JavaScript syntax was checked for the changed maintained browser runtime; and
- ZIP integrity was verified after packaging.

A Windows licensed-machine runtime test remains required after `Prepare-DevExpressAssets.cmd`, because this environment deliberately does not restore or execute the .NET/DevExpress application.
