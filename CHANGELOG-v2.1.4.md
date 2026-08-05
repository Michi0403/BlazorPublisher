# PublisherStudio 2.1.4

PublisherStudio 2.1.4 repairs DevExpress browser-asset preparation for the pinned DevExtreme 25.2.8 toolchain.

## DevExpress asset preparation

- The npm package tree is copied with symbolic links dereferenced so published browser assets never depend on `node_modules`.
- `dx.all.js` and `dx.light.css` are resolved from the restored package layout and copied explicitly into the maintained `wwwroot/vendor/devextreme-dist` paths.
- A bounded package-layout search supports both the `devextreme-dist` package and the official `devextreme/dist` fallback layout without downloading unpinned CDN files.
- Required assets are size-validated and recorded in `devextreme-assets.meta.json` with SHA-256 hashes and their resolved package source.
- Release publishing and MSBuild publish validation require the new client-asset metadata file.
- The private DevExpress license remains on the licensed build machine. Only the generated public runtime-license file is copied into PublisherStudio.

## Compatibility

- DevExtreme remains pinned to 25.2.8.
- The LocalGPT wire protocol remains pinned independently to 2.1.1.
- No application static state, CDN runtime dependency, or installer dependency was introduced.

## Build-policy repair

The existing architecture, logging, ownership, installer, publish, and documentation guards remain enabled. This patch changes the DevExpress preparation contract and its focused tests rather than weakening a gate.

## Application language selection

The LocalGPT-style PublisherStudio application language selector and JSON catalog structure introduced in 2.1.2 remain unchanged. The dependency-light installer console still has no application localization dependency.
