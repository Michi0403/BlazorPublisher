This directory is prepared by `Prepare-DevExpressAssets.cmd` on a licensed developer/build machine.

The preparation restores the pinned `devextreme-dist`, Spreadsheet, and jQuery npm packages. It also resolves the exact matching `devextreme@<version>` package through `npx` once and uses that one package for two related jobs: its `bin/devextreme-license.js` creates the public non-modular runtime key, and its `dist` browser runtime is overlaid into `wwwroot/vendor/devextreme-dist`. This keeps the runtime key generator and the browser runtime on the same DevExtreme package even if `devextreme-dist/package.json` contains stale internal metadata.

License generation is owned by `Prepare-DevExpressAssets.ps1`. The Node asset copier does not guess where the runtime key was generated and does not create license-version claims. The generated key is first written to a temporary build-machine path, validated, and only copied to `wwwroot/vendor/devextreme-license.js` after browser-asset preparation succeeds. Preparation metadata records the exact generator package version and SHA-256 of the generated public key without storing the private DevExpress license.

Published installations include the prepared files and run fully offline. The generated runtime-key script, generated vendor packages, and `node_modules` are intentionally excluded from source control and clean source archives.
