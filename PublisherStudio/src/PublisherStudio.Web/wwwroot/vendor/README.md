This directory is prepared by `Prepare-DevExpressAssets.cmd` on a licensed developer/build machine.

The preparation restores the pinned npm browser packages, copies the local DevExtreme, Spreadsheet, and jQuery files, and uses the official matching `devextreme-license` CLI to create `devextreme-license.js`. That generated file contains the public/runtime key used by the application and standalone HTML exports; it does not contain the private DevExpress license.

Published installations include the prepared files and run fully offline. The generated runtime-license script and `node_modules` are intentionally excluded from source control and clean source archives.
