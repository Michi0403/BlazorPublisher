# PublisherStudio 4.4.2 — source validation

Source baseline: PublisherStudio 4.4.1. Purpose: unblock the macOS release preflight and bring its same compatibility/storage check into normal Debug builds.

- XML parses: `Directory.Build.targets`, `PublisherStudio.Web.csproj`, `PublisherStudio.InstallerConsole.csproj`.
- Profile/cache contract: `Join-Path $documentationToolCacheRoot 'browser-profiles'` appears in the actual Chromium profile setup; the obsolete `Join-Path ... 'bp'` is absent. Process lifetime, unique names and cleanup logic are preserved.
- Guard wiring: `AssertPublisherPowerShellCompatibility` is a non-design-time Web `BeforeBuild` target; the existing maintenance target follows it. Direct `Build-LocalDevelopment.ps1` calls the same preflight. The PowerShell guard itself now checks both paths.
- Node regressions: browser-view inspection, Picture Studio canvas, GIF frames (see completed test output).
- No GitHub access or .NET build was used. No changes to the remaining architecture enforcement scripts or to the application/editor features.

**NOT VERIFIED:** execution of `pwsh build/Assert-PowerShellCompatibility.ps1`, `pwsh Build-Release.ps1`, a compiled macOS Debug build, and a macOS native release. These must run on the host with PowerShell and the licensed .NET toolchain.
