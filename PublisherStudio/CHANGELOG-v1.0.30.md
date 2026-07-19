# PublisherStudio v1.0.30 — Spreadsheet build and compiler fixes

## Fixed

- Moved `SpreadsheetEditorResult` from trailing Razor markup into `SpreadsheetEditorResult.cs`, fixing CS0246 in both `SpreadsheetEditor.razor` and `Editor.razor`.
- Added a committed npm lockfile and changed Spreadsheet asset restore to deterministic `npm ci --legacy-peer-deps`. PublisherStudio consumes the prebuilt `devextreme-dist` browser files rather than the full DevExtreme source package, so unused transitive packages such as deprecated `lodash.isequal` are no longer restored.
- Removed the unconditional `npm install` command from the normal Visual Studio/MSBuild path.
- Ordinary `Build`, `Rebuild`, F5, and design-time builds no longer fail with `MSB3073` / exit code `9009` when npm is not available in Visual Studio's inherited `PATH`.
- Added `Prepare-SpreadsheetAssets.cmd` and `Prepare-SpreadsheetAssets.ps1` as an explicit one-time source setup step.
- The preparation script locates Node.js/npm through `PATH`, NVM for Windows, and the standard system and per-user Node.js installation directories.
- Added a Node.js major-version check with a direct explanation when Node.js 20 or newer is not installed.
- Spreadsheet npm restoration and copying into `wwwroot/vendor` now happen in one command with verification of every required JavaScript and CSS file.
- Normal builds emit one actionable warning when the generated browser assets are absent instead of aborting the complete PublisherStudio build.
- Publish remains strict and stops with a clear message if Spreadsheet browser assets were not prepared, preventing creation of an incomplete offline package.
- `Build-Release.ps1` now uses the same preparation script, so command-line and Visual Studio release workflows behave identically.
- CI can opt back into automatic preparation with `-p:PrepareSpreadsheetAssetsOnBuild=true`.

## First build from source

Run this once from the solution root:

```powershell
.\Prepare-SpreadsheetAssets.cmd
```

Then build normally in Visual Studio or with `dotnet build`.

Node.js is a source-development/build-machine dependency required only while restoring the DevExpress Spreadsheet browser packages. It is not required on machines that run a completed PublisherStudio release, and the resulting application continues to serve all Spreadsheet assets locally without a CDN.

## Compatibility

- DevExpress package line remains `25.2.8`.
- Publication file format remains `1.29`; this release changes build tooling only and requires no document migration.
