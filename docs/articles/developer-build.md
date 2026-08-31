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

The release lane is host-aware. Windows always produces the Windows application/setup outputs. With the default `-WslLinux Auto`, an already-ready WSL distro also produces Linux x64/ARM64 Full/Light packages headlessly; if WSL is missing or unready, Windows continues normally. Run `Setup-WslLinuxBuild.ps1 -Provision` for explicit one-time Ubuntu/Debian provisioning. Native Linux remains supported, while macOS retains its native macOS lane and can also cross-publish Linux as before. `-Runtime all-rids` remains an explicit cross-host attempt, but DMG/signing/notarization are macOS-native finalization tasks.

The Windows parent prepares DevExpress assets and documentation once. The WSL child mirrors source to the Linux filesystem, reuses those prepared assets, consumes the LocalGPT-owned release-packaging package through the local-first resolver, and imports Linux packages into the same release bundle. RPM/AppImage are optional unless `-RequireOptionalNativePackages` is supplied; Docker/Podman is never required and is used only with `-UseContainerPackaging`.

## Quality gates

The maintained build checks architecture, diagnostics, localization, InteractiveServer render modes, async continuations, component boundaries, text ownership, iterator policy, system-variable initialization, JavaScript diagnostics, publish profiles, installer workflow, XML documentation coverage, and documentation output.

## Release record

Each release keeps one concise changelog and one validation report. Old work diaries and personal progress notes are not part of the public documentation. Completed, partial, and deferred work is summarized only when it helps users or maintainers understand the shipped state.
