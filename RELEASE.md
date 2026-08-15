# PublisherStudio 2.7.0 shared Local Chat localization cleanup

PublisherStudio 2.7.0 removes the incorrect `ChatGPT`/`LocalChatGPT` wording from the shared localization catalogs inherited by the integrated LocalGPT-facing UI vocabulary. PublisherStudio itself did not expose the problematic home card in the inspected source, so no unrelated editor behavior was changed.

The version rolls from 2.6.9 to 2.7.0 because the project version policy does not permit a `2.6.10` release. Browser module cache-busters are advanced with the application version.

## Versions

- PublisherStudio.Web: 2.7.0
- PublisherStudio.InstallerConsole: 2.7.0
- LocalGPT wire protocol: 2.1.1 (unchanged)
- publication format: unchanged

## Compatibility notes

- Publisher-started Council sessions remain durable LocalGPT `/chat` sessions.
- No editor, Panel Studio, media-studio, render-mode, or 1-Wire behavior was intentionally changed.
- The Japanese startup/default-language quirk was not modified.
- No GitHub access, `dotnet`, MSBuild, Visual Studio build, publish, or package restore was used to validate this source archive. The Windows build remains authoritative.
