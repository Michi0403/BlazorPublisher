# BlazorPublisher / PublisherStudio 2.1.9

PublisherStudio is maintained in [`PublisherStudio/`](PublisherStudio/). It is a local-first desktop publishing workbench for documents, spreadsheets, presentations, graphics, audio, video, streaming, and reusable interactive panels.

## Release contract

The current release uses these exact runtime assets:

- application archives: `winx64.zip`, `winx86.zip`, `winarm64.zip`, `linx64.zip`, `linarm64.zip`, `macosx64.zip`, and `macosarm64.zip`;
- setup archives: the same runtime names with the `setup` prefix;
- Windows installation root: `%LOCALAPPDATA%\PublisherStudio`, containing sibling runtime folders such as `winx64` and `setupwinx64`.

The setup downloads and validates both exact assets before modifying the installation. The authoritative LocalGPT wire dependency remains `LocalGPT.WireProtocolVersion` **2.1.1** and is independent from the PublisherStudio application version.

## Build and documentation

Run the maintained commands from `PublisherStudio/`:

```powershell
.\Build-LocalDevelopment.ps1
.\Build-AllRuntimes.ps1
.\Update-GitHubPagesSnapshot.cmd
```

GitHub Pages automation is intentionally stored at repository root in [`.github/`](.github/), because GitHub Actions does not discover workflows nested inside the product directory. The workflow publishes the tracked, validated Kawaii documentation snapshot.

See [`PublisherStudio/README.md`](PublisherStudio/README.md), [`PublisherStudio/RELEASE.md`](PublisherStudio/RELEASE.md), and [`PublisherStudio/VALIDATION.md`](PublisherStudio/VALIDATION.md).
