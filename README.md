# PublisherStudio

PublisherStudio is a local-first publishing studio for documents, stories, pictures, spreadsheets, presentations, websites, video, streaming, and reusable interactive panels.

Version **2.1.9** completes the release-facing installer, archive, documentation, and GitHub Pages contracts. It preserves the established LocalGPT-shaped runtime mapping, AppData layout, shortcut behavior, bounded diagnostics, and optional 1-Wire integration while adding strict asset and archive validation before installation.

## A gentle first path

1. Run `Prepare-DevExpressAssets.cmd` once on a licensed development machine.
2. Run `Build-LocalDevelopment.cmd` for a checked Debug build.
3. Start PublisherStudio and open **Help** in the ribbon.
4. Create a publication, add a page, and place content in the Mainframe.
5. Use Preview before exporting or publishing.

## Optional LocalGPT organic wiring

The app listens on `http://127.0.0.1:58071` by default. LocalGPT is optional. PublisherStudio listens for discovery messages on UDP `51141`, and only opens the 1-Wire transport on TCP `51140` after the user enables and approves the connection.

The organic adaptation system consumes the authoritative `LocalGPT.WireProtocolVersion` NuGet package. There is no `src/LocalGPT.WireProtocolVersion` directory or private protocol fork in this repository.

## One-click installation and update

PublisherStudio installs beneath:

```text
%LOCALAPPDATA%\PublisherStudio
```

The setup follows the same wrapper-based deployment shape as LocalGPT. Double-clicking the setup downloads the matching application and setup ZIPs, extracts both into the PublisherStudio AppData root, ensures FFmpeg is available, creates the required Desktop and Start Menu shortcuts, and starts the application.

The maintained shortcuts are:

- PublisherStudio Install
- PublisherStudio Update
- PublisherStudio Start
- PublisherStudio Folder

The installer uses no alternate product root and does not reuse an older product-name folder.

## Documentation

A normal Windows build generates the documentation shipped with a release:

- Kawaii DocFX website under `wwwroot/help-docs`;
- versioned `PublisherStudio-<version>.pdf`;
- complete API reference from compiler XML comments;
- `documentation-status.json` for the app and GitHub Pages workflow.

Inside PublisherStudio, open **Help** to launch the HTML guide, PDF book, API reference, or documentation status page. GitHub Pages deploys the same validated pinned Kawaii snapshot model as LocalGPT. Run `Update-GitHubPagesSnapshot.cmd` after a successful documentation build to refresh the tracked snapshot.

## Build and release

```text
Prepare-DevExpressAssets.cmd
Build-LocalDevelopment.cmd
Build-Release.cmd
Build-AllRuntimes.cmd
```

The release lane validates architecture, localization, diagnostics, XML documentation, publish profiles, installer launchers, browser assets, generated documentation, and archive wrapper paths before packaging.

## Architecture

- Runtime behavior is owned by dependency-injected services and host boundaries.
- Shared serializable data belongs to `PublisherStudio.BusinessObjects`.
- Application statics and static convenience factories are rejected by maintained build guards.
- LocalGPT wire compatibility is provided by the separately versioned `LocalGPT.WireProtocolVersion` package.
- Optional integrations do not prevent PublisherStudio from starting or publishing.

## License and third-party software

PublisherStudio is licensed under Apache-2.0. DevExpress components require a valid DevExpress license on development and build machines. Generated public runtime-license assets are included in release output; private license material is never copied into the application.

See `LICENSE`, `LICENSE.MD`, and `THIRD-PARTY-NOTICES.md`.


The Pages updater maintains both the GitHub Actions snapshot and the repository-root `/docs` `.nojekyll` mirror, so publication does not depend on whether the repository is currently configured for Actions or branch-based Pages. Missing DocFX namespace landing pages are materialized before strict link validation.
