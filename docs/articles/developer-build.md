# Developer build

## Requirements

- .NET 10 SDK
- Windows PowerShell 5.1 or newer for the maintained scripts
- Python 3 for version-matched GitHub Pages snapshot validation and seeding
- Node.js for preparing licensed DevExpress browser assets and documentation PDF generation
- A licensed DevExpress build identity for preparing runtime assets

## First source build

```
.\Prepare-DevExpressAssets.cmd
.\Build-LocalDevelopment.ps1 -Configuration Debug
```

The preparation command restores the pinned browser packages and generates the public runtime license. The private DevExpress license remains on the build machine.

## Release build

```
.\Build-Release.ps1 -Runtime win-x64
```

All maintained runtimes for the current host OS can be built sequentially with:

```
.\Build-AllRuntimes.ps1
```

The release lane is host-aware: Windows produces Windows application/setup outputs only; macOS produces macOS x64/ARM64 plus Linux x64/ARM64 application packages; Linux remains available as the native Linux release lane. On macOS, Linux TAR.GZ/DEB use the LocalGPT-owned managed packaging helper and RPM can use `rpmbuild` from Homebrew's `rpm` formula (`brew install rpm`). `-ProvisionNativePackagingTools` may install that formula only when explicitly requested and Homebrew is already present. `Build-Release.ps1 -Runtime all-rids` is available for an explicit cross-host publish attempt. AppImage remains Linux-native unless `-UseContainerPackaging` opts into an already-installed Docker/Podman engine. RPM/AppImage are optional unless `-RequireOptionalNativePackages` is supplied. The lane restores the authoritative LocalGPT wire protocol package, resolves the LocalGPT-owned release-packaging helper only when Unix/macOS packaging needs it, validates configuration and documentation, then creates the supported archives.

## Quality gates

The maintained build checks architecture, diagnostics, localization, InteractiveServer render modes, async continuations, component boundaries, text ownership, iterator policy, system-variable initialization, JavaScript diagnostics, publish profiles, installer workflow, XML documentation coverage, and documentation output.

## Release record

Each release keeps one concise changelog and one validation report. Old work diaries and personal progress notes are not part of the public documentation. Completed, partial, and deferred work is summarized only when it helps users or maintainers understand the shipped state.
