# DevExtreme runtime license in PublisherStudio

PublisherStudio uses DevExtreme 25.2.8 as a non-modular browser bundle. The main editor, Spreadsheet Studio, and every exported standalone HTML file are separate browser documents. Each document must register the generated public DevExtreme runtime key after `dx.all.js` loads and before the first DevExtreme component is constructed.

## Licensed developer/build-machine setup

From the solution root on Windows:

```powershell
.\Prepare-DevExpressAssets.cmd
```

The command performs these steps:

1. Verifies Node.js 20+, npm, and npx.
2. Restores the pinned `devextreme-dist`, Spreadsheet, and jQuery packages.
3. Reads the required DevExtreme version from `package.json`.
4. Runs the matching official CLI package:

   ```text
   npx --package devextreme@25.2.8 --yes devextreme-license --non-modular ...
   ```

5. Lets the CLI obtain the private DevExpress license from the registered build identity or `DevExpress_License`.
6. Atomically replaces `wwwroot/vendor/devextreme-license.js` only after generation succeeds.
7. Validates the generated public runtime script and writes exact-version metadata.

The private DevExpress license is not printed, copied into the project, or embedded in HTML. The generated public runtime key is the browser-side distributable artifact.

## Script order

PublisherStudio enforces this order everywhere:

```html
<script src="jquery.min.js"></script>
<script src="dx.all.js"></script>
<script src="devextreme-license.js"></script>
<script src="publisher-runtime.js"></script>
```

Standalone HTML export inlines the same scripts in the same order. It also compares the version marker generated with the runtime key against the version header in the bundled `dx.all.js` and stops the export if they differ.

## Build and publish behavior

A normal developer build can still compile with a warning when client assets are not prepared. Publishing is stricter: it fails when the runtime script, metadata, exact-version marker, or Spreadsheet browser files are missing. `Build-Release.ps1` always runs the preparation step before publishing.

The generated files are excluded from clean source archives and source control:

```text
wwwroot/vendor/devextreme-license.js
wwwroot/vendor/devextreme-license.meta.json
wwwroot/vendor/devextreme-license.version
```

They are included automatically in licensed publish output and embedded into exported standalone HTML.

## CI

Register the DevExpress license on the build agent or provide it through the protected `DevExpress_License` environment variable. Never place that private value in `appsettings.json`, JavaScript, the repository, a publication file, or an exported webpage.

Then run:

```powershell
.\Prepare-DevExpressAssets.ps1
dotnet publish src\PublisherStudio.Web\PublisherStudio.Web.csproj -c Release
```

## Troubleshooting

- A missing runtime key usually surfaces as DevExtreme warning `W0019`.
- A key generated for another DevExtreme version usually surfaces as `W0020`.
- Supplying the private/.NET key directly to browser configuration is not a replacement for the generated public runtime key.
- When preparation fails, the script leaves an existing generated runtime key untouched and reports whether Node.js, package restore, license discovery, or generation failed.
