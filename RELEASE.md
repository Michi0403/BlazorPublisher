# PublisherStudio 4.1.4

PublisherStudio 4.1.4 keeps the forward baseline at DevExpress/DevExtreme **25.2.10** and .NET **10.0.12**, and makes the existing DevExpress browser-asset provisioning path automatic for normal builds.

A clean source tree no longer compiles first and leaves Spreadsheet/HTML export assets missing. The build invokes `Prepare-DevExpressAssets.ps1 -EnsureCurrent` before compilation: already-current 25.2.10 assets return immediately, while missing/stale/mixed/hash-invalid assets are restored, licensed and seeded through the maintained PowerShell pipeline.

The 4.1.1 lifecycle diagnostics and 4.1.2 visibility repair remain intact.

See `CHANGELOG-v4.1.4-AUTOMATIC-DEVEXPRESS-ASSET-PROVISIONING.md` and `VALIDATION-v4.1.4-source.md`.
