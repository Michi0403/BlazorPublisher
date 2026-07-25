# Media conversion and Panel/Div Studio doctrine

PublisherStudio v1.0.78 adds two reusable platform capabilities rather than isolated editor features.

## Optional FFmpeg media conversion

Browser decoding is intentionally not treated as a universal media layer. HTML does not require every browser to support every audio/video container and codec combination, and image support also differs by format and browser generation. PublisherStudio therefore keeps original imported media intact and offers an explicit conversion path for browser-oriented derivatives.

The converter invokes a separately installed `ffmpeg` executable through `ProcessStartInfo.ArgumentList`; it does not concatenate a shell command, upload media, or bundle an FFmpeg binary. The executable can be discovered on `PATH`, through the existing PublisherStudio FFmpeg locator, or configured with `PublisherStudio:FFmpegPath` / `PUBLISHERSTUDIO_FFMPEG`.

The service/controller boundary is deliberately reusable:

- `IMediaConversionService` owns capability discovery, jobs, progress, cancellation and output streams.
- `MediaConversionService` is the local FFmpeg adapter.
- `MediaConversionController` exposes the same operations to browser clients and future components.
- `MediaConverterStudio` is one frontend consumer; PictureStudio, VideoStudio, Mainframe drops and streaming tools can reuse the same service instead of spawning FFmpeg themselves.

The initial presets are browser-oriented WebM VP9/VP8 with Opus, optional MP4 H.264/AAC, Ogg Opus, WAV PCM, PNG, lossless/lossy WebP and AVIF. Presets are enabled only when the installed FFmpeg reports the required encoders. The original source is never silently deleted.

FFmpeg's official legal page states that the normal project license is LGPL 2.1-or-later while optional GPL components change the obligations of a specific build. PublisherStudio therefore does not redistribute or claim a license for the user's executable. Users must select a build suitable for their distribution and codecs.

Primary references:

- FFmpeg legal and licensing: https://ffmpeg.org/legal.html
- FFmpeg codecs: https://ffmpeg.org/ffmpeg-codecs.html
- FFmpeg formats: https://ffmpeg.org/ffmpeg-formats.html
- FFmpeg downloads: https://ffmpeg.org/download.html
- MDN image formats: https://developer.mozilla.org/en-US/docs/Web/Media/Guides/Formats/Image_types
- MDN video codecs: https://developer.mozilla.org/en-US/docs/Web/Media/Guides/Formats/Video_codecs

## Panel/Div Studio

A panel is not a foreign dashboard engine. `PanelElement` is a normal `PublicationElement` containing ordered `PublicationPanelView` objects, and each view contains the same polymorphic publication elements used on a page. This keeps one authoring and rendering model for:

- Mainframe pages
- reusable panels and nested panels
- live KPI dashboards
- DataVisual and DevExtreme components
- media and live streaming inputs
- isolated HTML/CSS/optional JavaScript experiences
- print, standalone HTML and structured website exports

`PublicationElementTraversal` recursively walks panels, allowing persistence, media asset registration and normalization to include nested content. A maximum normalization depth prevents malicious or accidental unbounded nesting.

`HtmlEmbedElement` uses an iframe sandbox as its trust boundary. Scripts, forms, popups, same-origin access and top navigation are independent opt-ins. Importing an HTML file does not automatically enable its scripts.

## Panel library and live dashboards

The optional Panel Library is stored as editor view state and can be opened or hidden without modifying panel content. Presets are factories, not opaque binary templates. They create normal publication elements and can be edited immediately:

- blank reusable panel
- live KPI dashboard
- operations board
- creator/gamer hub
- multi-view web experience

The KPI dashboards share `PublicationDataObject` and live-data wiring with ordinary page components. Panel navigation and local interactions are initialized by `componentRuntime.js`, so they continue to work in Mainframe preview and every HTML export mode.

## Compatibility and migration

Publication format 1.54 adds `PanelElement`, `PublicationPanelView`, `HtmlEmbedElement`, and `PanelLibraryVisible`. Older documents load normally. v1.0.78 normalizes missing panel IDs, view slugs, active views and nested element contracts. Older PublisherStudio versions do not understand the new polymorphic element discriminators and should not be used to resave v1.0.78 documents containing panels.
