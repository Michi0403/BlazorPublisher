# PublisherStudio 3.1.4 — Shared LocalGPT Packaging and Release Restore

## Release build repair

- Removed the duplicated `src/LocalGPT.ReleasePackaging` project from PublisherStudio. LocalGPT is now the authoritative source owner for this installer/release helper, matching the existing 1-Wire package ownership model.
- Reworked `Ensure-ReleasePackagingPackage.ps1` to resolve `LocalGPT.ReleasePackaging.<version>.nupkg` from:
  - an explicitly supplied LocalGPT repository,
  - `LOCALGPT_REPOSITORY`,
  - the shared `%LOCALAPPDATA%/LocalGPT/NuGet` cache,
  - or the LocalGPT release asset URL.
- Added package-shape validation before accepting cached/copied/downloaded release-packaging packages.
- Tool installation now uses an isolated local NuGet configuration and intentionally does not use `--add-source`, fixing the reported package-source-mapping failure.
- SHA-256 manifest writing no longer uses the PowerShell 7-only `utf8NoBOM` encoding token; it writes BOM-free UTF-8 through .NET so the Windows PowerShell 5.1 release launcher remains valid.
- Added `ReleasePackagingVersion`, `ReleasePackagingPackageUrl`, and `RefreshReleasePackagingPackage` release-build controls without changing the existing 1-Wire controls.

## Preserved application behavior

- The reported Windows runtime trace shows PublisherStudio reaches the running host, initializes the runtime policy/services, records a browser video, applies it to the timeline, and exports a single-file website; these paths are not rewritten by this maintenance release.
- Existing PublisherStudio `InteractiveServer` page/island boundaries and the seven-RID Windows/macOS/Linux release matrix remain unchanged.
- The prior system-variable, XML-documentation, Debug documentation/Pages, and operator-policy fixes remain present.

## Versioning

- Application, installer, npm metadata, documentation, and frontend cache-busting identities move from 3.1.3 to 3.1.4.
- The one-digit minor/patch slot policy remains enforced.
