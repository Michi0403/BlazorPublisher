# PublisherStudio 4.1.4 — automatic DevExpress 25.2.10 asset provisioning

## Summary

PublisherStudio now treats the prepared DevExpress/DevExtreme browser payload as a versioned build prerequisite instead of a manual post-clone step. Normal non-design-time builds automatically run the maintained PowerShell provisioning preflight and seed the exact DevExpress/DevExtreme **25.2.10** runtime when the prepared payload is missing, stale, mixed-version, incomplete, or hash-invalid. .NET remains **10.0.12**.

## Fixed

- Changed the normal `PublisherStudio.Web` build default so `PrepareSpreadsheetAssetsOnBuild` is enabled unless an explicitly delegated build sets it to `false`.
- `PrepareSpreadsheetClientAssets` now invokes `Prepare-DevExpressAssets.ps1 -EnsureCurrent -ExpectedVersion $(DevExpressVersion)` on every ordinary non-design-time build.
- Added a fast ensure-current path to `Prepare-DevExpressAssets.ps1`. Valid 25.2.10 assets return immediately without npm/npx/license regeneration.
- Missing or stale assets automatically fall through to the existing maintained restore/license/seed pipeline.
- The provisioning script now compares `PublisherStudio.Web.csproj` / the build-supplied `DevExpressVersion` against both `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` before provisioning. Mixed version declarations fail explicitly instead of seeding an ambiguous runtime.
- The fast preflight validates the generated runtime-key version and hash, DevExtreme asset metadata, authoritative runtime package version, copied package versions, required browser files, and prepared `dx.all.js` / `dx.light.css` sizes and SHA-256 values.
- The compatibility `Prepare-SpreadsheetAssets.ps1` alias forwards the ensure-current/version parameters as well.
- Updated developer documentation so a first normal build is sufficient; `Prepare-DevExpressAssets.cmd` remains available as an explicit manual refresh.

## Preserved

- DevExpress/DevExtreme remains **25.2.10** everywhere in the active source tree.
- .NET package/tooling components remain **10.0.12**.
- 4.1.1 Panel Studio lifecycle diagnostics and the 4.1.2 explicit visibility repair remain unchanged.
- The private DevExpress license remains build-machine-only; only the generated public runtime key is copied to the application payload.

## Version

`4.1.3 -> 4.1.4`, following the maintained single-digit version-slot rule.
