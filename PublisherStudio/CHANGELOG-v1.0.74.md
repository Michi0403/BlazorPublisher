# PublisherStudio v1.0.74 changelog

## Structured static website export

- Retains both existing standalone HTML export modes and adds **Export structured website (ZIP)** to the File application tab in the `DxRibbon`.
- The structured export expands to `index.html`, separate CSS and JavaScript files, content-addressed media/font assets, an export manifest, and a concise deployment README.
- Website and interactive-presentation behavior are selectable from one export dialog and reuse the matching standalone runtime.
- Assets are deduplicated by content hash. One source embedded in several publication objects is written once and referenced through relative paths.
- The generated site is ordinary static content. It can be opened locally or hosted without Blazor, ASP.NET Core, Node.js, a package manager, or a build step.

## Lossless packaging and media options

- **Preserve source — exact/lossless** is the default for pictures, video, and audio. It removes Base64 embedding overhead without re-encoding the source bytes.
- **PNG — lossless pixels** provides a predictable browser-decoded raster option. SVG and GIF remain untouched so vector content and animation are not flattened.
- **WebP** and **AVIF** are optional lossy picture-delivery optimizations. Conversion is accepted only when the browser returns the requested format and the result is smaller; otherwise the source is preserved and a warning is reported.
- AVIF encoding capability is detected at export time. Browsers without an AVIF Canvas encoder fall back to WebP, then to the source.
- **WebM VP9/VP8 + Opus** is an optional local browser transcode using `captureStream()` and `MediaRecorder`. It is explicitly not described as lossless and is accepted only when smaller.
- Optional original-video fallback keeps the source beside an optimized WebM. The publication media runtime automatically switches to it after an optimized-source playback error.
- FFV1 is documented as a genuine archival lossless video codec that is unsuitable for direct native website playback. FFmpeg.wasm is documented as a future optional codec-pack path and is not bundled in this release.

## C# and JavaScript ownership

- `Editor.razor` owns the export dialog, managed options, progress/notice state, live-data refresh, recovery save, and result reporting.
- `publisherStudio.exportStructuredWebsite` owns browser DOM cloning, standalone-runtime projection, data-URL extraction, Canvas/MediaRecorder conversion, SHA-256 addressing, path rewriting, ZIP construction, and download.
- JavaScript returns file name, asset count, embedded-source byte estimate, archive size, and warnings through a typed Blazor interop result.
- Browser-side processing remains local. The exporter performs no media upload and no automatic external-resource fetch.

## Archive behavior

- ZIP generation now supports method 8 Deflate for HTML, CSS, JavaScript, JSON, and text through `CompressionStream('deflate-raw')` when available.
- The writer automatically uses STORE when Deflate is unavailable or does not save space.
- Images, video, audio, fonts, and other already compressed assets are stored directly to avoid wasteful recompression.
- Existing ZIP consumers keep the original STORE-only wrapper and behavior.

## Runtime and compatibility

- The structured exporter first invokes the existing single-file builder. CSS, ordered DevExtreme/runtime scripts, interactions, animations, components, live-data runtime, media sequence behavior, and Signal Connectors therefore remain one implementation.
- Relative paths are used throughout the generated site. Live REST/OData sources remain external and still require access to their configured endpoints.
- Project/publication schemas are unchanged: publication format remains `1.52` and Picture Studio format remains `1.4`.
- Application, installer, streaming runtime capability, npm package, and lock-file version is `1.0.74`.
- NuGet and npm dependency sets remain unchanged.
