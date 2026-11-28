# PublisherStudio 2.8.1 source changelog

## Build repair

- Fixed the PublisherStudio 2.8.0 XML documentation coverage gate that stopped the build before C# compilation.
- Added the missing XML documentation block for `PublicationAdaptiveQualityProfile` and `PublisherMediaSessionDefaultsPolicy`.
- Added the required `<value>` documentation for the adaptive recording/streaming policy properties introduced or extended in 2.8.0.
- Added the missing primary-constructor/record parameter documentation and method parameter/return documentation in `MediaQualityRecommendationService`.
- Added XML documentation for the private `FitWithin` and `Positive` helpers because the repository policy intentionally covers maintained private members too.
- Added the missing LAN audio bitrate `<value>` contract.
- Replaced the small set of summaries rejected as generic by the repository documentation-quality policy with contextual summaries.

## Retained behavior

- No recording, streaming, layer, timeline, publication, 1-Wire, provider, adaptive-media, or capture-quality behavior was removed or intentionally changed.
- The adaptive quality/configuration work from 2.8.0 remains intact.
- The explicit `InteractiveServer` component boundaries remain unchanged.

## Version

- PublisherStudio.Web: 2.8.1
- PublisherStudio.InstallerConsole: 2.8.1
- Browser module cache-busters: 2.8.1

This package is source-only and was not compiled with .NET by the assistant.
