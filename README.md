# PublisherStudio

PublisherStudio is a local-first publishing studio for documents, stories, pictures, spreadsheets, presentations, websites, video, streaming, and reusable interactive panels.

Version **3.2.4** keeps the current application, editor, documentation, recording/export, and installer behavior while expanding the host-aware release lane. Windows builds the Windows application/setup matrix only; macOS builds macOS x64/ARM64 plus Linux x64/ARM64 packages in one run; Linux remains available for native Linux release work. Linux TAR.GZ/DEB packaging is managed, RPM can use Homebrew `rpm` on macOS, and AppImage remains an optional Linux/container finisher.

## Build

1. Run `Prepare-DevExpressAssets.cmd` on a licensed development machine.
2. Run `Build-LocalDevelopment.cmd` for Debug verification.
3. Run `Build-Release.cmd` for the owner-side release lane.

The solution is `src/PublisherStudio.sln`. Generated/licensed DevExpress browser assets are intentionally excluded from clean source packages.

## Documentation

The maintained Markdown is under `docs/`. A normal owner build creates one Kawaii DocFX tree in `wwwroot/help-docs`, including the complete XML-generated API reference and the versioned HTML-backed PDF. `Update-GitHubPagesSnapshot.cmd` validates that generated tree and refreshes `.github/pages/publisherstudio-kawaii-docs.zip`. GitHub Pages publishes that exact validated snapshot through Actions; the authored `docs/` directory is never overwritten with generated output.

The in-app Help page opens the same HTML, PDF, API reference, and status routes in the focus-managed documentation viewer.

## Install and update

The release setup uses `%LOCALAPPDATA%\PublisherStudio`, exact runtime assets, wrapper validation, and maintained Install/Update/Start/Folder shortcuts. LocalGPT and organic 1-Wire integration remain optional.

## License

Apache-2.0. DevExpress components require a valid DevExpress build license. See `LICENSE`, `LICENSE.MD`, and `THIRD-PARTY-NOTICES.md`.
