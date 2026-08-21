# PublisherStudio 2.9.4 — DevExtreme Runtime-Key Provenance Repair

## DevExtreme preparation correctness

- Corrected the 2.9.3 preparation guard that treated `node_modules/devextreme-dist/package.json` as the authoritative restored version. The npm lock already pins the requested `devextreme-dist` archive and integrity; stale internal package metadata is now diagnostic rather than a false hard failure.
- `Prepare-DevExpressAssets.ps1` now resolves one exact `devextreme@25.2.9` package through `npx` and verifies that package's own `package.json` before any runtime-key or browser-runtime work.
- The generated public runtime key is produced by that exact package's `bin/devextreme-license.js`; the script no longer asks a separate Node helper to guess or validate the runtime-key output path.
- License generation writes first to a unique build-machine temporary directory. The key is copied to `wwwroot/vendor/devextreme-license.js` only after browser-asset preparation succeeds.
- Runtime-key metadata now records the exact generator package/version and SHA-256 of the generated public key. The version marker represents the **generator package version**, not a claim that the license itself "targets" a patch version.
- The same exact `devextreme@25.2.9` package supplies the authoritative `dist` overlay for the browser runtime. This keeps `dx.all.js`, themes, localization and vector-map runtime data aligned with the package that supplied `devextreme-license` even when `devextreme-dist` contains stale internal metadata.
- Generated DevExtreme/Spreadsheet/jQuery vendor targets continue to be cleared with Windows-friendly retry/rename handling before preparation.

## Runtime and export loading order

- The generated non-modular DevExtreme runtime-key script now loads immediately after `dx.all.js` in `App.razor`, before `<Routes />` can instantiate application components.
- Standalone HTML export uses the same order: jQuery → `dx.all.js` → generated runtime key → DevExtreme map/runtime helpers → PublisherStudio runtimes.
- Standalone export validates the prepared asset manifest and SHA-256 values, validates the generated runtime-key SHA-256, and checks the exact generator/runtime package provenance. It no longer treats `devextreme-dist/package.json` or a synthetic version marker as proof of what a license "targets".
- Browser runtime files remain versioned with `?v=25.2.9`, while export fetches prepared files with `cache: no-store`.

## Existing 2.9.3 functionality retained

- Panel Studio object addresses, declarative behaviors, common component methods, right-click behavior actions and JavaScript helper UX remain intact.
- Translation/localization work from 2.9.2 remains intact.
- Target framework remains `net10.0`; DevExpress/DevExtreme remains 25.2.9.
- No database/schema migration was introduced.
