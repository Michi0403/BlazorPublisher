# PublisherStudio 3.1.3 — build maintenance and cross-platform release review

## Versioning

- Increased PublisherStudio from 3.1.2 to **3.1.3**, preserving the one-digit minor/patch slot policy.
- Updated Web/installer-console versions, npm package identity, documentation metadata, browser asset cache-busting versions, and dynamic module cache-busting versions.

## Build and maintenance repairs

- Removed direct system-variable string-key access from `OrganicReplayPolicyDataService` and `PublisherRuntimePolicyDataService`. The four operator runtime maxima now flow through typed `ISystemVariableStoreService` properties, with key ownership and permissive `Int32.MaxValue` defaults centralized in `SystemVariableStoreService`.
- Added the missing XML `<value>` documentation for `OrganicReplayPolicyDataService.Snapshot` and completed XML documentation for the repository-owned `LocalGPT.ReleasePackaging` program/members. The full C#/Razor XML-documentation coverage gate now passes statically.
- Fixed the Debug GitHub Pages synchronization failure reported under `Set-StrictMode`: zero PDF results no longer dereference a missing `.Name` property.
- Debug builds now explicitly validate HTML-only documentation when `pdfAvailable=false` and leave the tracked release Pages ZIP unchanged. Release builds still require exactly one current versioned PDF and continue the full snapshot verification/update path.
- Normalized repository-relative paths in `Update-GitHubPagesSnapshot.ps1`, `Build-Release.ps1`, and `Build-LocalDevelopment.ps1` so release/development source and asset probes are host-neutral on Windows, macOS, and Linux.
- Hardened release-documentation PDF diagnostics against `Set-StrictMode` failures when the candidate collection is empty.

## Cross-platform review against the supplied 3.0.0 baseline

- Preserved all four explicit `@rendermode InteractiveServer` page boundaries from the supplied PublisherStudio 3.0.0 source baseline.
- PublisherStudio.Web remains `net10.0` with all seven maintained runtime identifiers: `win-x64`, `win-x86`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`.
- Windows/Unix runtime behavior stays behind DI platform boundaries, and the maintained 60-check cross-platform audit reports no platform leaks.
- Windows remains the only setup-console publishing path. macOS/Linux release lanes publish application payloads directly without a Windows-style setup console.
- macOS Full + Light lanes retain `.app`/`.tar.gz` and native `.dmg` finishing via Apple `hdiutil`.
- Linux Full + Light lanes retain `.tar.gz`, `.deb`, `.rpm`, and `.AppImage`, executable-mode restoration, the FFmpeg licensing/dependency boundary, and versioned SHA-256 checksums.
- The repository-owned `LocalGPT.ReleasePackaging` tool continues to own TAR.GZ, DEB, and checksum materialization; `dpkg-deb` is not required. RPM/AppImage continue to prefer native packaging tools and can use Docker/Podman fallback when necessary.

## Validation boundary

The handoff is source-only as requested. This environment has no .NET SDK or PowerShell runtime, so no compiler success is claimed here. Maintained Python source/architecture/async/resilience/component/XML/cross-platform/release audits are run against a fresh extraction of the exact delivered ZIP. See `VALIDATION-v3.1.3-source.md`.
