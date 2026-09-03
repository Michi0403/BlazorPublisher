# PublisherStudio

PublisherStudio is an open-source, local-first .NET 10 and Blazor publishing environment for documents, stories, pictures, spreadsheets, presentations, websites, video, streaming, and reusable interactive panels. It is designed to remain useful as a standalone application while optionally cooperating with LocalGPT and organic 1-Wire peers.

## Future2 role

PublisherStudio is the creative/productivity side of the broader **Future2** direction: user-owned software and AI infrastructure that does not require a centralized corporate or government service to own the user's data, tools, or workflow. Cloud and web services can be integrated where useful, but local operation and human authority remain first-class.

Together, LocalGPT and PublisherStudio demonstrate that the same open .NET foundation can cover both AI orchestration and ordinary creative/productivity software. The intended scale ranges from one person's workstation to independently operated local infrastructure, with explicit interfaces for cooperating systems rather than mandatory platform lock-in.

## Build

The solution is `src/PublisherStudio.sln`. Generated/licensed DevExpress browser assets are intentionally excluded from clean source packages. The maintained build entry points have both Windows `.cmd` wrappers and cross-platform PowerShell scripts.

```powershell
# Windows
.\Prepare-DevExpressAssets.ps1
.\Build-LocalDevelopment.ps1

# macOS / Linux
pwsh ./Prepare-DevExpressAssets.ps1
pwsh ./Build-LocalDevelopment.ps1

# Owner/release lane
pwsh ./Build-Release.ps1
```

A licensed development/build environment is required for the maintained DevExpress-based UI. Current .NET package restore uses NuGet.org plus the repository-local LocalGPT wire-protocol cache where applicable; the private DevExpress developer license is a separate build-time identity and is not included in source ZIPs or end-user installations.

## Documentation

The maintained Markdown is under `docs/`. A normal owner build creates one Kawaii DocFX tree in `wwwroot/help-docs`, including the complete XML-generated API reference and the versioned HTML-backed PDF. `Update-GitHubPagesSnapshot.cmd` / its PowerShell counterpart validates that generated tree and refreshes `.github/pages/publisherstudio-kawaii-docs.zip`. GitHub Pages publishes that exact validated snapshot; the authored `docs/` directory is never overwritten with generated output.

The in-app Help page opens the same HTML, PDF, API reference, and status routes in the focus-managed documentation viewer.

## Install and update

The release setup uses the canonical per-user PublisherStudio root, exact runtime assets, wrapper validation, writable user-data locations, and maintained Install/Update/Start/Folder shortcuts. macOS builds native `.app`, DMG and PKG forms; Linux/Windows package lanes remain part of the same release coordinator. LocalGPT and organic 1-Wire integration remain optional.

## License

PublisherStudio project-owned source is Apache-2.0. The maintained UI uses proprietary DevExpress components under DevExpress's separate terms. The repository grants no DevExpress license and does not ship the private developer license. See `LICENSE`, `LICENSE.MD`, and `THIRD-PARTY-NOTICES.md` for the exact project/third-party boundary.
