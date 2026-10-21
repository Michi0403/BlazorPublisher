# PublisherStudio 4.2.9 — Picture Studio raster, vector, GIF and layer regression repair

Full source release based directly on PublisherStudio 4.2.8. The GIF feature is preserved, not removed or made a requirement for ordinary pictures.

## Repaired image-canvas ownership

- `PictureEditor.razor.cs`: GIF images now open immediately as ordinary editable raster pictures. Animated-frame decoding is an optional asynchronous enhancement. Failures retain the still-image raster rather than leaving an empty transparent canvas. GIF additions use the same existing insert/replace path as ordinary raster images, with the image's position preserved during promotion to timed layers.
- `PictureDocumentService.CreateFrameRenderProjection`: avoids sending the full multi-frame, potentially hundreds-of-megabytes GIF BusinessObject across Blazor JS interop on **every** render. The source PictureDocument retains the full canonical layer graph, all frames, delays, ordering and dimensions; the projection carries only the active frame and ordinary/shared layers. It has no persistence side effect.
- `pictureStudioInterop.js`: the shared asynchronous renderer now renders into a staging canvas before committing the most recent frame. This prevents races between image/SVG decode, mouse edits, selection changes and successive renders from clearing or overwriting a newer frame. Errors are reported through the existing diagnostics callback, rather than silently producing a blank export.
- Canvas layer hit testing and layered SVG visual export now respect the active GIF frame; non-active frames do not invisibly intercept selections or overlay the current SVG image. Non-animated layers are unaffected.

## Preserve existing picture and layer workflows

- GIF import no longer removes **all** previously animated layers from a picture document. The single-timeline model explicitly refuses to replace an existing GIF timeline with another, instead retaining existing document content and the new raster fallback. This is a documented limitation, not silent data loss.
- The original inserted/replaced GIF raster's position, dimensions, rotation, opacity and stacking order are carried into its independently editable frame layers.
- Standard PNG, JPEG and WebP edit/render paths remain in place. SVG files opened through ordinary Picture Studio flows use the editable SVG layer importer rather than being permanently flattened to a raster. SVGs whose shapes cannot be safely decomposed retain the sanitized original vector markup as one editable SVG layer, with a warning. Raster fallback is retained where SVG decoding/import fails.
- GIF Apply and Download stage rendered frames one at a time, then use the existing GIF89a encoder, instead of sending the whole animated document in one oversized JS interop invocation. Shared images/paint/effects remain part of every rendered animation frame. Apply continues to use the already existing bounded callback chunks into InteractiveServer and preserves the source document for **Apply layered**.
- GIF frame navigation, durations, transparency, merged output and GIF89a editing/export remain available. Existing DevExpress controls, localization catalogs, publication formats and safety checks remain unchanged.

## Build and validation limits

- Maintained browser JavaScript manifest is refreshed for the changed JS source, with the existing PowerShell guard intact. Versioned browser imports, PublisherStudio Web and setup product version are set to 4.2.9.
- `node --check` passed for the Picture Studio modules; a mocked asynchronous render test proved that a stale pending frame cannot overwrite a newer one; animated GIF encoding passed Node GIF89a verification and an independent Pillow decoder check for three frames and their original delays.
- Windows/.NET/DevExpress UI compilation and live browser execution are **not verified** in this container. This is a source-level repair, not a claim that the Windows interactive regression has been reproduced end-to-end here.
