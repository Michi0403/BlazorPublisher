# PublisherStudio 3.0.3 - Cross-platform Node/DevExpress asset runtime resolution

PublisherStudio 3.0.3 fixes the next macOS release-build regression exposed after the 3.0.2 Python-runtime repair: `Prepare-DevExpressAssets.ps1` required a non-null Windows candidate-path array before it could even try `Get-Command node`, so a Unix host with no Windows Node installation folders failed during PowerShell parameter binding.

## Build tooling repair

- `Prepare-DevExpressAssets.ps1` now uses the shared `build/NodeRuntime.Common.ps1` resolver instead of maintaining a Windows-only Node discovery path.
- The asset-preparation stage resolves or provisions the pinned verified Node.js 22.23.2 runtime with the same Windows/macOS/Linux logic used by documentation generation.
- The resolved Node directory is added to the process `PATH`; adjacent `npm`/`npx` (`npm.cmd`/`npx.cmd` on Windows) are preferred, with host-appropriate PATH fallbacks.
- `Resolve-Executable` now accepts an empty candidate collection, so command discovery is never blocked by a null platform-specific candidate array.
- `Assert-SourcePackagePrerequisites.ps1` now resolves Node.js before the ordered release work and reports the exact selected runtime, complementing the Python preflight introduced in 3.0.2.
- DevExpress asset preparation reports the Node runtime it actually uses before invoking npm/npx.

## Scope

The 3.0.1 backend Windows/Unix service boundaries, System.Drawing/GDI cleanup, DocFX/Pages accessibility flow and the 3.0.2 host-neutral Python resolver remain unchanged. No GitHub remote state is modified by these source changes.
