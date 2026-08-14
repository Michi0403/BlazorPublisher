# PublisherStudio 2.6.9 changelog

## LocalGPT session continuity

- PublisherStudio-started Council requests now explicitly remain durable LocalGPT `/chat` sessions. The bridge no longer lets a remote `SaveToMemory` preference make a Publisher-started team/round disappear from LocalGPT history.
- This pairs with LocalGPT 2.8.6, which retains provider-supplied reasoning, function calls/results, live Council output, partial failures, and Council markdown diagnostics across the 1-Wire boundary.
- No PublisherStudio rendering, media, Panel Studio, localization, publication-format, or installer behavior is reverted by this change.

## Release policy

- PublisherStudio.Web: 2.6.9.
- PublisherStudio.InstallerConsole: 2.6.9.
- Publication format: 1.58 (unchanged).
- Picture Studio format: 1.5 (unchanged).
- 1-Wire protocol: 2.1.1 (unchanged).
- Existing InteractiveServer boundaries are unchanged.
