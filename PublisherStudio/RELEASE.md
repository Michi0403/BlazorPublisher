# PublisherStudio v1.0.37 release

See `CHANGELOG-v1.0.37.md`.

Source release notes:

- Run `Prepare-DevExpressAssets.cmd` after extracting the source package on a machine with Node.js 20+ and a registered valid DevExpress license. The historical `Prepare-SpreadsheetAssets.cmd` command remains an alias.
- The preparation restores local npm browser assets and invokes `devextreme-license` from the matching `devextreme@25.2.8` package to generate a non-modular public runtime key.
- The private DevExpress license is never copied into PublisherStudio or into exported HTML. Published installations and HTML exports contain only the generated public/runtime key.
- `dotnet publish` and `Build-Release.ps1` now stop when the runtime-license file or its version metadata is missing.
- The main application, Spreadsheet Studio, and standalone HTML export register the runtime key after `dx.all.js` and before any component/runtime initialization.
- Standalone HTML remains self-contained: DevExtreme CSS, jQuery, DevExtreme, the generated runtime license, and PublisherStudio's live-data runtime are inlined.
- End-user installations do not require Node.js, npm, npx, internet access, or a private DevExpress license.
- Publication document format remains `1.36`; this release changes the build/export runtime, not the stored document schema.
