# PublisherStudio architecture

## Stable boundaries

- **PublisherStudio.Web** remains the product host. It owns ASP.NET Core loopback hosting, Interactive Blazor Server, DevExpress integration, the publication model, export interop, services, and local API endpoints.
- **PublisherStudio.InstallerConsole** remains an optional deployment helper with no project reference to the web UI. It installs or launches published output and can publish a source ZIP without Git.
- **WinUI remains optional and absent from this solution.** Nothing in the editor requires a desktop shell.

The v0.2 work extends these boundaries; it does not replace the host, routing, dependency injection, controllers, or installer wiring.

## Editing engines

- DevExpress `DxRibbon` is the primary command surface.
- DevExpress `DxContextMenu` provides object/page commands on right-click.
- DevExpress `DxRichEdit` edits the rich-text story stored by a selected text frame.
- The publication page is an absolute-positioned HTML object surface. A small native JavaScript module performs pointer tracking, resize, crop, snapping, ruler rendering, guide manipulation, and browser export.
- C# remains authoritative. JavaScript only previews an active pointer operation and commits final millimetre values through JS interop.

## Canvas view model

`PublicationViewSettings` stores user-facing canvas state separately from page content:

- ruler unit and visibility
- grid and guide visibility
- grid spacing
- snap-to-grid, snap-to-guides, and snap-to-page
- raster export DPI

The page itself stays millimetre-based. Zoom changes only the conversion from millimetres to CSS pixels. Rulers derive their ticks from the live page rectangle and therefore follow zoom, scrolling, viewport size, and page format.

## Picture model

Picture editing is non-destructive. The original image data URL remains unchanged while the model stores crop translation, crop scale, picture rotation, frame fit mode, filters, opacity, flips, mask, border, and shadow. Interactive crop gestures update the DOM for immediate feedback and commit to the model at gesture completion.

## Export pipeline

- JSON is generated from the C# model and downloaded through Blazor stream interop.
- PNG/JPEG clone the current page, remove editor-only adorners, serialize it into an SVG `foreignObject`, render that SVG to a browser canvas at the selected DPI, and download the resulting bitmap.
- SVG downloads the same self-contained current-page representation.
- Website export clones the hidden multi-page print representation into a self-contained HTML document with embedded image data.
- Print/PDF uses the hidden print representation and the browser print system.

The pipeline adds no runtime package. A future production exporter can be introduced behind an export service without changing the editor model.

## File model

A `.pubstudio.json` file contains:

- document metadata and view settings
- pages and guides
- polymorphic text, image, and shape elements
- RichEdit HTML bytes for text stories
- self-contained image data URLs

Format version `1.1` remains backward-compatible with the previous file because absent view/picture properties receive defaults during deserialization.

## Reference and license boundary

GIMP and Inkscape are behavioural references only. No source from those projects is copied or linked. This avoids coupling the Apache-2.0 application to their GPL implementation while still applying familiar interaction principles such as ruler-origin page coordinates, draggable guides, non-destructive adjustments, and crop-inside-frame editing.

## Security boundary

Imported preview HTML is stripped of active elements, event-handler attributes, and `javascript:` URLs before rendering. RichEdit document bytes remain the editable story source. Image imports are limited to supported browser image MIME types and bounded stream sizes.
