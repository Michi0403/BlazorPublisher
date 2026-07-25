# PublisherStudio v1.0.78 changelog

## Reusable Panel/Div Studio

- Added first-class `PanelElement` and sandboxed `HtmlEmbedElement` publication components.
- Added Panel/Div Studio with multiple views, local navigation, nested panels, shared PublisherStudio components, live cameras, KPI/chart/table components, DevExtreme components and standalone HTML import.
- Added an optional Panel Library with blank, KPI dashboard, operations, creator/gamer and web-experience presets.
- Added recursive element traversal so nested media, data, components and panels participate in persistence and asset registration.
- Added panel rendering to Mainframe, print, standalone HTML and structured website exports.
- Added panel navigation and local interaction initialization to the shared component runtime.

## Optional FFmpeg Media Converter Studio

- Added reusable `IMediaConversionService`, local `MediaConversionService`, and `/api/media-conversion` controller endpoints.
- Added FFmpeg capability and encoder discovery, queued jobs, progress, cancellation, download and insertion into Mainframe.
- Added browser-oriented presets for WebM, optional MP4, Ogg Opus, WAV, PNG, WebP and AVIF.
- FFmpeg remains a separately installed optional executable; no binary or new package dependency is bundled.
- Process arguments use `ArgumentList` with shell execution disabled.

## Persistence and release

- Advanced publication format to `1.54` for panels and HTML experiences.
- Advanced application, installer, npm, lock, streaming runtime and structured export manifest versions to `1.0.78`.
- Added architecture doctrine and regression contracts for both feature groups.
