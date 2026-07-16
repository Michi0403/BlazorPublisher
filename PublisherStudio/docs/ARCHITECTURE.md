# PublisherStudio architecture

## Stable boundaries

- **PublisherStudio.Web** remains the ASP.NET Core loopback host and Interactive Blazor Server application. It owns DevExpress integration, publication state, browser interop, controllers, and exports.
- **PublisherStudio.InstallerConsole** remains an optional deployment helper with no UI project dependency. It can install/start published output or publish a source ZIP without Git.
- **WinUI remains optional and absent.** The browser host is the product core.

v0.5 extends these boundaries without replacing routing, controllers, the installer, or the publication editing model. Picture Studio is an additional scoped subsystem within the existing web host.

## Editing engines

- DevExpress `DxRibbon` is the main command surface.
- DevExpress `DxContextMenu` provides page/object right-click workflows.
- DevExpress `DxRichEdit` owns rich story editing, DOCX persistence, fields, page layout, printing, and DOCX/RTF/TXT/HTML downloads.
- The publication surface is an absolute-positioned HTML/SVG canvas. Native JavaScript performs pointer previews, rulers, guides, crop gestures, snapping, connector reconnection, and browser export.
- C# is authoritative. JavaScript commits final millimetre values/endpoints through JS interop.


## Picture Studio subsystem

Picture Studio is deliberately separate from both the page surface and RichEdit. `PictureEditorStateService` owns a scoped `PictureDocument`, selection, history, and layer operations. `PictureDocumentService` owns polymorphic JSON cloning/normalization. The `PictureEditor` Blazor component presents the shell and properties, while `pictureStudioInterop.js` owns Canvas 2D drawing, hit testing, direct transforms, procedural rendering, and raster output.

Supported layer types are raster, text, shape, fill, and procedural render. Every layer shares bounds, rotation, opacity, blend mode, lock/visibility, and non-destructive adjustment values. Procedural layers store parameters and seeds, not generated pixel buffers. The renderer currently provides Clouds, Noise, Stripes, and Vignette.

When Picture Studio applies its result, JavaScript returns a PNG as `IJSStreamReference`; Blazor reads the stream and stores the rendered data URL in the normal `ImageFrameElement.DataUrl`. A cloned `PictureDocument` is stored in `ImageFrameElement.PictureSource`. This dual representation keeps all established publication rendering/export code simple while allowing later non-destructive Picture Studio edits. Imported pictures have no `PictureSource` until first applied through Picture Studio.

## Workspace and view model

`PublicationViewSettings` stores rulers, unit, grid/guides, snapping, zoom-related preferences, and raster DPI separately from page content. The five-column workspace allocates fixed/resizable side panes and gives all remaining width to the canvas; panes can collapse without overlaying the rulers.

The page stays millimetre-based. Zoom only changes millimetres-to-CSS-pixels conversion. Rulers derive their origin and ticks from the live page rectangle, so they follow zoom, scrolling, viewport changes, and page dimensions.

## Object/layer model

Every publication object is a layer with visibility, lock state, and Z index. Supported polymorphic elements are text frame, image frame, shape, WordArt, and connector. The Layers UI is a direct view over that list rather than a second layer subsystem.

## Picture model

Picture editing is non-destructive. `OriginalDataUrl` retains the imported source, including PNG alpha. The active model stores crop, scale, image rotation, fit/fill, opacity, CSS adjustments, flips, mask, border, shadow, tint/recolor mode, blend mode, and color-key transparency parameters. Color-key removal produces a new PNG data URL while Restore Original remains available.

## Connector model

A connector stores source and target element IDs plus one of eight anchors per endpoint. Geometry is resolved from live object bounds and rotation. Straight, elbow, and cubic paths are generated without a third-party diagram package. During move/resize, JavaScript updates attached paths immediately; the C# model remains authoritative after commit. Reconnection hides the existing path and only displays a temporary path when a valid target port is within the snap radius. Invalid release restores the old endpoints.

Connectors are ordinary polymorphic publication elements for ordering, visibility, lock state, serialization, duplication, thumbnails, export, and print. Deleting an object removes its attached connectors.

## RichEdit story migration

v0.1/v0.2 stored story bytes as HTML, which limited Office page-layout and formatting commands. The loader detects legacy HTML. On first editor open, RichEdit exports it to Office Open XML and recreates the component in DOCX mode. New stories start as a minimal valid DOCX package. The canvas stores a sanitized HTML preview beside the DOCX source.

## Export pipeline

- JSON uses Blazor stream interop.
- SVG clones the current page and removes all editor-only adorners.
- PNG/JPEG serialize that clone into an SVG `foreignObject`, then rasterize using `createImageBitmap`, object-URL Image, or data-URL Image fallback. PNG uses an alpha-enabled canvas; JPEG receives a white fill.
- Website export clones the multi-page print surface into a self-contained HTML file.
- Print/PDF uses the hidden print surface and browser print system.
- Story downloads are produced directly by DevExpress RichEdit as DOCX, RTF, TXT, or HTML.

No runtime package was added. A future server-side prepress exporter can sit behind an export service without changing the editor model.

## File model

A `.pubstudio.json` file contains document/view metadata, pages, guides, polymorphic elements, DOCX story bytes plus sanitized previews, embedded image data URLs, and optional editable Picture Studio layer documents. Current format version is `1.6`; the loader supplies defaults and migrates older story, image, and WordArt path fields.

## Reference and license boundary

GIMP and Inkscape are behavioural references for familiar image/ruler workflows. Blazor.Diagrams is a behavioural reference for ports, snapping, and reconnection. No code or runtime dependency is copied from any of them, preserving the Apache-2.0 boundary.

## Security boundary

Imported preview HTML is stripped of active elements, event-handler attributes, and `javascript:` URLs before rendering. Image MIME types and stream sizes are bounded. RichEdit document bytes are treated as the editable story source rather than injected HTML.
