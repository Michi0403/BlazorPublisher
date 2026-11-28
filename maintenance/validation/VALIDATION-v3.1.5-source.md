# PublisherStudio 3.1.5 Source Validation

This source package is statically validated in an environment without the .NET SDK or a PowerShell runtime. The user's Windows runtime trace remains evidence that the PublisherStudio application can start and exercise recording/export behavior; the next user release build remains the authoritative end-to-end package check.

## Shared packaging repair checked

- PublisherStudio still has no duplicate `LocalGPT.ReleasePackaging` source project.
- The authoritative packaging `.nupkg` is resolved from LocalGPT checkout/cache/release locations and installed with an isolated NuGet configuration.
- `dotnet tool install` progress output is host-only so the captured helper result contains only the executable path.
- `Build-Release.ps1` requires exactly one returned path and verifies it before Linux/macOS native packaging starts.

## Installer/release matrix checked

- Windows: PublisherStudio installer console/application setup lanes remain intact.
- Linux: Full/Light TAR.GZ/DEB/RPM/AppImage packaging remains setup-console-free.
- macOS: Full/Light `.app`/TAR.GZ packaging and native DMG finishing remain setup-console-free.
- PublisherStudio continues to consume the LocalGPT-owned packaging helper similarly to the shared 1-Wire package.

The release, architecture, async, service/component-resilience, iterator, prerender/interop, Panel Studio persistence, cross-platform, structured-file and documentation audits are rerun on the final source tree and again after extracting the exact ZIP.
