# PublisherStudio v1.0.37 — DevExtreme runtime-license and standalone export repair

## Correct standalone HTML licensing

- Fixed the regression where an exported `.html` file opened as a separate browser application and recreated DevExtreme charts without registering a runtime key on that page.
- Standalone website export now loads and inlines scripts in the required order: jQuery, `dx.all.js`, the generated non-modular DevExtreme runtime-license file, PublisherStudio live-data code, and finally the presentation initializer.
- Export refuses to create a misleading file when the generated runtime-license asset is missing, malformed, or generated for a different DevExtreme version, and reports the exact preparation command to run.
- The public/runtime key is also registered immediately after DevExtreme in the main PublisherStudio page and in the isolated Spreadsheet Studio page.

## Integrated Node.js asset pipeline

- Added `Prepare-DevExpressAssets.cmd` / `.ps1` as the authoritative client-asset preparation command. `Prepare-SpreadsheetAssets.cmd` remains a compatibility alias.
- The preparation restores the pinned local npm packages, then invokes the official `devextreme-license` CLI from `devextreme@25.2.8` through npx.
- The CLI generates a non-modular `wwwroot/vendor/devextreme-license.js` from the DevExpress license registered on the build machine or supplied through `DevExpress_License`.
- The private DevExpress license is not written to the project, publish directory, or exported HTML. Only the generated public/runtime key is distributed.
- Added generated metadata and a plain version marker that record the exact DevExtreme version used by the key generator. Preparation, publish, and HTML export reject mismatched versions.
- Browser assets remain local and offline at runtime. Node.js, npm, and npx are build-machine tools only.

## Build and release safeguards

- Normal developer builds issue a clear warning when DevExpress browser assets or the generated runtime key are missing.
- `dotnet publish` now fails when the Spreadsheet assets, runtime-license script, or runtime-license metadata/version marker are absent.
- `Build-Release.ps1` runs the new preparation pipeline and verifies every required asset before publishing installers.
- Generated runtime-license files are excluded from source control/source archives, while licensed publish output includes them automatically as static web assets.

## Compatibility

- Application/package version updated to `1.0.37`.
- DevExpress and DevExtreme remain pinned to `25.2.8`.
- Publication document format remains `1.36`; no stored publication schema changed in this release.
