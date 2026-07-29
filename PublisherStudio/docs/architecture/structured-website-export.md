# Structured website export doctrine (v1.0.74)

## Purpose

PublisherStudio retains the existing standalone HTML exports and adds a structured website export for publications whose embedded media makes one HTML file impractically large. The structured export is downloaded as a ZIP and expands to an ordinary static website:

```text
index.html
css/site.css
js/
  publisher-runtime.js
  component-runtime.js
  live-data-runtime.js
  vendor/...
assets/
  images/...
  video/...
  audio/...
  fonts/...
  files/...
publisherstudio-export.json
README.txt
```

`index.html` contains relative references only. No application server, Blazor runtime, build step, package manager, or remote PublisherStudio service is required to display the exported publication. It can be opened locally or copied to a static web host. Live REST/OData bindings still require access to the endpoints configured by the publication.

## Why splitting the HTML is already lossless

A standalone PublisherStudio export stores local media as `data:` URLs. Base64 represents each three source bytes with four encoded characters, before HTML markup and quoting. This Base64 overhead is therefore roughly one third over the source byte count. Moving those exact bytes into `assets/` removes that representation overhead without decoding or re-encoding the media.

**Preserve source** is the default for pictures, video and audio. It is byte-preserving and is the only generally honest lossless choice for arbitrary browser-playable video. The website ZIP may Deflate HTML, CSS, JavaScript, JSON and text; already compressed image, audio and video assets are stored without redundant ZIP recompression.

Content-addressed filenames are derived from a SHA-256 digest. Identical embedded assets are written once and reused by every referencing publication object.

## Picture options

### Preserve source — exact/lossless

The original source bytes and MIME type are externalized. SVG and GIF are always preserved in v1.0.74 so vector structure and animation are not accidentally rasterized.

### PNG — lossless pixels

The browser decodes the picture to a canvas and writes PNG. The resulting pixels are lossless relative to the browser-decoded image, but source metadata, original compression, color-profile details not retained by Canvas, and vector structure are not preserved. This is useful for a predictable web-safe raster, not as an archival replacement for the original source.

### WebP — optional lossy optimization

The WebP specification supports both lossless and lossy coding. Browser Canvas export, however, does not expose a portable switch that guarantees the lossless WebP encoder mode. PublisherStudio therefore labels its Canvas-created WebP option **lossy** and keeps the source when the conversion is unsupported or larger.

### AVIF — optional lossy optimization

AVIF is attempted only when the current browser can actually encode `image/avif`. If the Canvas encoder does not return AVIF, PublisherStudio tries WebP and records a warning. The original is retained when neither conversion works or when the result is larger. Browser decoding support and browser encoding support are separate capabilities.

## Video options

### Preserve source — exact/lossless

The source container and codec bytes are copied unchanged. This is the default and the only v1.0.74 option described as lossless. Whether a source plays remains a browser/container/codec compatibility question; restructuring the website cannot make an unsupported source codec playable.

### WebM VP9/VP8 + Opus — optional lossy optimization

WebM is an open web media container. The exporter asks `MediaRecorder.isTypeSupported` for VP9 + Opus, then VP8 + Opus, then generic WebM. A supported source is played into `captureStream()` and recorded locally. This is a real-time, lossy browser transcode. It is used only when the result is smaller than the original.

When **Keep the original video as a compatibility fallback** is selected, the optimized WebM and the original source are both exported. The publication runtime switches to the original if the browser reports a playback error for the WebM asset.

The exporter reports a warning and preserves the source when the browser cannot decode the input, cannot capture the media stream, lacks a compatible MediaRecorder encoder, produces no output, fails during conversion, or creates a larger result.

### Why FFV1 is not offered

FFV1 is a genuine lossless video codec and is excellent for archival workflows, but it is not a broadly native HTML `<video>` playback format. A website export must remain directly displayable in browsers, so v1.0.74 does not create an FFV1 asset and pretend it is web-compatible.

### Why FFmpeg.wasm is not bundled in this release

FFmpeg.wasm can transcode media entirely inside a browser and would broaden the input/output matrix. It also adds a substantial WebAssembly/worker payload and performs materially slower than native FFmpeg. v1.0.74 deliberately adds no new npm or native dependency and uses browser APIs already present in the running PublisherStudio client. A future optional codec pack may integrate a locally hosted, pinned FFmpeg.wasm build without changing the structured-site manifest contract.

## Blazor and JavaScript interop contract

The C# editor owns the user workflow and options through `StructuredWebsiteExportOptions`. It calls:

```text
publisherStudio.exportStructuredWebsite(fileName, title, options)
```

JavaScript owns DOM cloning, standalone-runtime parity, `data:` URL extraction, media encoding, content addressing, file generation and ZIP creation. It returns `StructuredWebsiteExportResult` with the archive name, asset count, embedded-source byte estimate, archive size and warnings. C# presents that result to the user.

This separation follows the Blazor WebAssembly boundary: publication state and commands stay in managed code; byte-oriented browser APIs such as Canvas, MediaRecorder, CompressionStream, Blob, URL and download activation stay in JavaScript.

## Runtime parity and fallback

The exporter first generates the existing single-file website or presentation HTML. It then parses that generated document and externalizes its style, scripts and embedded assets. This makes the structured output a packaging projection of the established runtime rather than a second rendering implementation.

Media-sequence runtime support reads `data-publisher-original-src`. If an optimized source fails, the runtime replaces it with the original, reloads metadata, and preserves the selected segment/playhead as far as the browser permits.

## Safety and privacy

- All processing occurs in the current browser tab.
- No media is uploaded by the structured exporter.
- Object URLs are revoked after conversion.
- Failed or unsupported optimization falls back to preserving source bytes.
- Generated file names are content-addressed and contain no source-path disclosure.
- External live-data endpoints are not copied into the archive; their existing publication configuration remains unchanged.

## Compatibility boundary

The structured export guarantees the same PublisherStudio publication runtime as the matching standalone export and an ordinary static file layout. It does not guarantee that every browser can decode every preserved source codec. Use Preserve source for fidelity, PNG for browser-decoded lossless raster pixels, WebP/AVIF for optional picture size reduction, and WebM for optional open lossy video delivery with the original fallback enabled when broad compatibility matters.

## Fact-check references

- Google WebP overview and lossless bitstream specification: https://developers.google.com/speed/webp and https://developers.google.com/speed/webp/docs/webp_lossless_bitstream_specification
- WebM Project format overview and container guidance: https://www.webmproject.org/about/ and https://www.webmproject.org/docs/container/
- MDN image/container/video codec guides: https://developer.mozilla.org/en-US/docs/Web/Media/Guides/Formats/Image_types, https://developer.mozilla.org/en-US/docs/Web/Media/Guides/Formats/Containers, and https://developer.mozilla.org/en-US/docs/Web/Media/Guides/Formats/Video_codecs
- FFmpeg.wasm overview and performance notes: https://ffmpegwasm.netlify.app/docs/overview/ and https://ffmpegwasm.netlify.app/docs/performance/
