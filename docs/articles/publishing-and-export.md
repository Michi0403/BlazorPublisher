# Publishing and export

PublisherStudio can deliver one project in several forms.

## Print and PDF

Print uses the publication page model and browser print surface. Choose **Print / Save as PDF** when the target is a fixed document.

## Images and SVG

PNG and JPEG are raster outputs. SVG keeps vector-friendly content where possible and freezes or marks media that cannot remain interactive.

## Websites

- **Interactive presentation HTML** keeps page navigation and interaction.
- **Single-file website** embeds the delivery payload into one HTML file.
- **Structured website ZIP** creates a normal file tree with content-addressed assets and the PublisherStudio runtime.

The structured exporter validates archive paths, preserves original media when optimization fails, and does not upload project data.

## Recorded presentation

Video export renders publication timing into a media file. Keep the native publication beside the export so the sequence can be revised later.

##### Tip

Publish for the audience, but save for your future self. The native project is the editable keepsake. 🎀

## Structured website media policy

Structured websites avoid Base64 overhead by writing normal content-addressed assets. Preserve source media whenever possible: the exporter prefers to keep it when the browser can use it safely. Optional conversions may target PNG, WebP, AVIF, WebM, or lossless FFV1 workflows, depending on the source and available tooling.

Browser conversion is coordinated through Blazor and JavaScript interop. FFmpeg.wasm can be an optional browser-side helper, but export does not depend on it: when optimization is unavailable or fails, PublisherStudio keeps the compatible source instead of damaging the publication.
