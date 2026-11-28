# PublisherStudio 4.0.2 source validation

## Scope

This release changes release-build storage ownership only. PublisherStudio runtime/editor behavior, including the confirmed 4.0.0 RichEdit race repair and 4.0.1 transient-state maintenance contract, remains intact.

## Confirmed source checks

- `build/audit_repository_build_storage.py` passed. The full release and standalone documentation entrypoints initialize repository-owned build storage before heavy work; dotnet/NuGet/npm/XDG/documentation/temp state is redirected under the checkout by default; and `NodeRuntime.Common.ps1` prefers repository fallback storage before per-user application data.
- Application architecture, async continuation policy, async-only component/service architecture, Razor maintenance architecture, component resilience, service resilience, cross-platform boundaries, and InteractiveServer transient UI-state ownership audits passed.
- The transient-state audit still confirms Story RichEdit live document/selection state is not server-rebound, Media Studio range drag commits on release, and the reviewed 47 native continuous-preview ranges remain browser-coalesced.
- The existing JavaScript diagnostics inventory remains unchanged by this build-script-only release; package JSON and project XML parse checks are part of the release audit.
- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.0.2**, preserving the single-digit minor/patch rule.

## Storage contract reviewed

`Build-Release.ps1` initializes `artifacts/.build-storage` unless `FUTURE2_BUILD_STORAGE_ROOT` is explicitly supplied. It redirects `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`, `NUGET_PLUGINS_CACHE_PATH`, `NUGET_SCRATCH`, `NPM_CONFIG_CACHE`, `XDG_CACHE_HOME`, `TMPDIR`, `TMP`, `TEMP`, and documentation cache ownership before restore/build/DevExpress preparation/documentation/native packaging work. Standalone `Build-Documentation.ps1` initializes the same contract.

## Environment limitation

`pwsh`/Windows PowerShell and the .NET SDK are not available in this preparation environment. Therefore no PowerShell runtime execution, `dotnet build`, restore, publish, DocFX execution, browser PDF render, notarization, or native package build was claimed. The Mac host remains the authoritative runtime verification for the redirected storage environment.
