# Developer build

## Requirements

- .NET 10 SDK
- Windows PowerShell 5.1 or newer for the maintained scripts
- Node.js for preparing licensed DevExpress browser assets and documentation PDF generation
- A licensed DevExpress build identity for preparing runtime assets

## First source build

```powershell
.\Prepare-DevExpressAssets.cmd
.\Build-LocalDevelopment.ps1 -Configuration Debug
```

The preparation command restores the pinned browser packages and generates the public runtime license. The private DevExpress license remains on the build machine.

## Release build

```powershell
.\Build-Release.ps1 -Runtime win-x64
```

All supported runtimes can be built sequentially with:

```powershell
.\Build-AllRuntimes.ps1
```

The release lane restores the authoritative LocalGPT wire protocol package, publishes the application and standalone setup, validates configuration and documentation, then creates manifest-backed archives.

## Quality gates

The maintained build checks architecture, diagnostics, localization, InteractiveServer render modes, async continuations, component boundaries, text ownership, iterator policy, system-variable initialization, JavaScript diagnostics, publish profiles, installer workflow, XML documentation coverage, and documentation output.

## Release record

Each release keeps one concise changelog and one validation report. Old work diaries and personal progress notes are not part of the public documentation. Completed, partial, and deferred work is summarized only when it helps users or maintainers understand the shipped state.
