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

The release lane is host-aware. **macOS is the broad coordinator:** the normal `-Runtime all` path builds macOS x64/ARM64, Linux x64/ARM64, and Windows x64/x86/ARM64 application/setup payloads in one run. Windows still produces its Windows outputs directly and, with the default `-WslLinux Auto`, an already-ready WSL distro also produces Linux x64/ARM64 Full/Light packages headlessly; missing/unready WSL is non-fatal. Run `Setup-WslLinuxBuild.ps1 -Provision` for explicit one-time Ubuntu/Debian provisioning. Native Linux remains fully supported. `-Runtime all-rids` remains the explicit all-RID request on any host.

The Windows parent prepares DevExpress assets and documentation once. The WSL child mirrors source to the Linux filesystem, reuses those prepared assets, consumes the LocalGPT-owned release-packaging package through the local-first resolver, and imports Linux packages into the same release bundle. Linux TAR.GZ/DEB are managed formats. RPM is created on native Linux and on macOS when `rpmbuild` is installed (including Homebrew `rpm`, optionally provisioned only with `-ProvisionNativePackagingTools`). AppImage remains Linux/WSL-native unless `-UseContainerPackaging` is explicitly requested. macOS Full/Light packages include TAR.GZ, DMG, and PKG where built-in `pkgbuild` is available. Docker/Podman is never required.

## Quality gates

The maintained build checks architecture, diagnostics, localization, InteractiveServer render modes, async continuations, component boundaries, text ownership, iterator policy, system-variable initialization, JavaScript diagnostics, publish profiles, installer workflow, XML documentation coverage, and documentation output.

## Release record

Each release keeps one concise changelog and one validation report. Old work diaries and personal progress notes are not part of the public documentation. Completed, partial, and deferred work is summarized only when it helps users or maintainers understand the shipped state.


## Pages snapshot size boundary

The tracked `.github/pages/publisherstudio-kawaii-docs.zip` is intentionally HTML-only. The full release PDF is still mandatory in the release documentation payload, but the Pages preparation step records its filename/size/tagging metadata, excludes the PDF from the tracked ZIP, and rewrites PDF links to the latest release. This keeps the Pages archive deterministic and below its extracted-size validation ceiling even when a browser/DocFX fallback PDF is very large.
