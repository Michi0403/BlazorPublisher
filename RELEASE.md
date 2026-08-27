# PublisherStudio 3.0.3

PublisherStudio 3.0.3 is the **Cross-platform Node/DevExpress Asset Runtime Resolution** maintenance release.

The 3.0.1 backend platform-boundary refactor remains unchanged: common services continue to consume neutral interfaces while Windows and Unix implementations are selected at dependency-injection composition time. The removal of unused `System.Drawing.Common`/GDI baggage and the cross-platform DocFX/Pages pipeline are preserved. The 3.0.2 host-neutral Python resolver is preserved as well.

This release fixes the next macOS release-build regression: `Prepare-DevExpressAssets.ps1` built only Windows-specific Node candidate arrays and declared `CandidatePaths` as a mandatory string array. On Unix those candidates can legitimately be empty, which caused PowerShell parameter binding to fail before normal command lookup could run.

DevExpress asset preparation now consumes the shared PublisherStudio Node runtime resolver, resolving or provisioning the pinned verified Node.js 22.23.2 distribution for Windows, macOS or Linux. npm/npx are resolved next to that runtime first, with host-appropriate PATH fallbacks. The initial source preflight now resolves Node.js as well, so a missing or unusable runtime is reported before the ordered build reaches asset preparation.

This handoff is source-only and was not built with .NET or PowerShell in the packaging environment. See `CHANGELOG-v3.0.3-NODE-DEVEXPRESS-ASSET-RUNTIME-RESOLUTION.md` and `VALIDATION-v3.0.3-source.md`.
