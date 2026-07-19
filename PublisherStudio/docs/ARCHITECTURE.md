# PublisherStudio architecture

## Stable boundaries

- **PublisherStudio.Web** remains the ASP.NET Core loopback host and Interactive Blazor Server application. It owns DevExpress integration, publication state, browser interop, controllers, and exports.
- **PublisherStudio.InstallerConsole** remains an optional deployment helper with no UI project dependency. It can install/start published output or publish a source ZIP without Git.
- **WinUI remains optional and absent.** The browser host is the product core.

The feature subsystems extend these boundaries without replacing routing, controllers, the installer, or the publication editing model. Picture Studio, Spreadsheet Studio, publication data visuals, media studios, and presentation animation all remain scoped parts of the existing web host.

## Editing engines

- DevExpress `DxRibbon` is the main command surface.
- DevExpress `DxContextMenu` provides page/object right-click workflows.
- DevExpress `DxRichEdit` owns rich story editing, DOCX persistence, fields, page layout, printing, and DOCX/RTF/TXT/HTML downloads.
- DevExpress ASP.NET Core Spreadsheet owns workbook editing, formulas, worksheets, and XLSX/XLSM/XLS/CSV/TXT compatibility. It is hosted as a same-origin MVC/Razor island inside the Blazor modal because DevExpress does not provide this control as a native Blazor component.
- DevExpress chart, pie, polar, sparkline, bar-gauge, grid, and progress components render publication data visuals directly on the page and in the print surface.
- The publication surface is an absolute-positioned HTML/SVG canvas. Native JavaScript performs pointer previews, rulers, guides, crop gestures, snapping, connector reconnection, and browser export.
- C# is authoritative. JavaScript commits final millimetre values/endpoints through JS interop.


## Spreadsheet Studio subsystem

`SpreadsheetElement` is a normal publication layer with bounds, rotation, Z order, visibility, lock state, frame styling, workbook bytes, file name/format, active sheet name, and a static worksheet preview. Imported or blank workbooks open in the same styled modal workflow as Story, Picture, Audio, and Video Studio. Double-clicking an existing spreadsheet frame creates an isolated editing session; Apply updates the selected layer, Download returns the current workbook, and Cancel removes a newly created pending frame.

The editing surface is the supported DevExpress ASP.NET Core Spreadsheet MVC/Razor helper, not a simulated Blazor grid. `SpreadsheetController` handles DevExpress document requests and custom saves. `SpreadsheetSessionStore` binds each modal session to a unique DevExpress document ID and expires abandoned sessions. The Blazor modal communicates with the same-origin iframe through a restricted `postMessage` bridge. Save waits for active-cell edits and Spreadsheet synchronization to finish before it sends the client state to the server.

`SpreadsheetDocumentService` creates minimal blank XLSX packages, validates imported workbook signatures, and generates an escaped static preview from the active OpenXML sheet or delimited text. The preview is used by the page canvas, thumbnails, print surface, and browser exports; executable workbook content is never injected into the publication DOM. XLS and edited delimited files are preserved on import but are saved back as XLSX after editing; XLSM remains XLSM. Licensed DevExpress and DevExtreme browser resources are restored during source build and copied into `wwwroot/vendor`, so a published installation runs without a CDN or internet connection.


## Picture Studio subsystem

Picture Studio is deliberately separate from both the page surface and RichEdit. `PictureEditorStateService` owns a scoped `PictureDocument`, selection, history, and layer operations. `PictureDocumentService` owns polymorphic JSON cloning/normalization. The `PictureEditor` Blazor component presents the shell and properties, while `pictureStudioInterop.js` owns Canvas 2D drawing, hit testing, direct transforms, procedural rendering, and raster output.

Supported layer types are raster, text, shape, fill, and procedural render. Every layer shares bounds, rotation, opacity, blend mode, lock/visibility, and non-destructive adjustment values. Procedural layers store parameters and seeds, not generated pixel buffers. The renderer currently provides Clouds, Noise, Stripes, and Vignette.

When Picture Studio applies its result, JavaScript returns a PNG data URL in bounded chunks; Blazor reassembles it and stores it in the normal `ImageFrameElement.DataUrl`. A cloned `PictureDocument` is stored in `ImageFrameElement.PictureSource`. This dual representation keeps all established publication rendering/export code simple while allowing later non-destructive Picture Studio edits. Imported pictures have no `PictureSource` until first applied through Picture Studio.

## Publication data and visual subsystem

`PublicationDataService` owns parsing, normalization, type inference, and projection of publication data into component-specific rows. `PublicationDataObject` is stored once at document level and can be reused by any number of visual elements. Supported source kinds are JSON, delimited text (CSV, TSV, semicolon, or pipe), and live document-object data. The live source projects page name, object name/type, position, dimensions, rotation, Z index, visibility, and lock state without duplicating those values into the file.

`DataVisualElement` is an ordinary polymorphic publication element. It stores the DevExpress visual kind, selected data-object ID, category/series/value fields, subtype, legend/label/title options, gauge/KPI range, and grid row settings. `DataManager` edits reusable sources; `DataVisualEditor` maps fields and previews the result; `DataVisualView` is shared by the page canvas and print surface. C# data objects remain authoritative, and no external JavaScript chart library is introduced.

The first visual set is deliberately publication-oriented: Cartesian chart, pie/doughnut, polar chart, sparkline, circular bar gauge, data grid, and KPI progress indicator. Maps are not included because useful map rendering generally requires an external GIS/tile provider and often an API key, which conflicts with the self-contained runtime rule. Sankey/dashboard/reporting components remain potential later additions after their document/export semantics are defined.

## Animation and interaction subsystem

Animation remains part of the authoritative C# document model. Each `PublicationElement` owns ordered semantic `PublicationAnimation` records and one `PublicationInteraction`; each page owns a `PublicationPageTransition`; the document owns `PublicationPlaybackSettings`. The animation order is page-wide rather than local to an element, which lets the inspector present one deterministic timeline across text, images, shapes, WordArt, connectors, and data visuals.

The browser preview and website exporter map semantic effects to Web Animations API keyframes. Trigger groups preserve page-entry, with-previous, after-previous, and click behavior. Interactions support page navigation, safe URL opening, target visibility, and replay. `HiddenAtPresentationStart` is separate from editor-layer visibility so an object remains editable while being initially hidden during presentation playback.

The model stores no CSS keyframes or PowerPoint-specific XML. This is deliberate: PowerPoint and video exporters can consume the same normalized timeline and map it to their own timing/rendering systems. See `docs/ANIMATION_EXPORT.md`.

## Workspace and view model

`PublicationViewSettings` stores rulers, unit, grid/guides, snapping, zoom-related preferences, and raster DPI separately from page content. The five-column workspace allocates fixed/resizable side panes and gives all remaining width to the canvas; panes can collapse without overlaying the rulers.

The page stays millimetre-based. Zoom only changes millimetres-to-CSS-pixels conversion. Rulers derive their origin and ticks from the live page rectangle, so they follow zoom, scrolling, viewport changes, and page dimensions.

## Object/layer model

Every publication object is a layer with visibility, lock state, and Z index. Supported polymorphic elements are text frame, spreadsheet frame, image frame, audio/video frame, shape, WordArt, connector, barcode, and data visual. The Layers UI is a direct view over that list rather than a second layer subsystem.

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
- Website export clones the multi-page print surface into a self-contained animated presentation with a dependency-free playback runtime, transitions, click groups, interactions, controls, and print fallback.
- Print/PDF uses the hidden print surface and browser print system.
- Story downloads are produced directly by DevExpress RichEdit as DOCX, RTF, TXT, or HTML.
- Spreadsheet frames render their escaped static worksheet preview in page, print, image, SVG, website, and video export surfaces; workbook downloads remain native XLSX/XLSM files.

The web project now references `DevExpress.AspNetCore.Spreadsheet` and copies its Spreadsheet, DevExtreme, and jQuery browser assets into the published `wwwroot`. A future server-side prepress exporter can still sit behind an export service without changing the editor model.

## File model

A `.pubstudio.json` file contains document/view metadata, pages, guides, polymorphic elements, DOCX story bytes plus sanitized previews, embedded spreadsheet workbook bytes plus regenerated static previews, embedded image/media data, and optional editable Picture Studio layer documents. Current format version is `1.34`; the loader supplies defaults and migrates older story, spreadsheet, image, media, WordArt path, data-object, data-visual, animation, transition, interaction, and playback fields.

## Reference and license boundary

GIMP and Inkscape are behavioural references for familiar image/ruler workflows. Blazor.Diagrams is a behavioural reference for ports, snapping, and reconnection. No code or runtime dependency is copied from any of them, preserving the Apache-2.0 boundary.

## Security boundary

Imported preview HTML is stripped of active elements, event-handler attributes, and `javascript:` URLs before rendering. Image MIME types and stream sizes are bounded. RichEdit document bytes are treated as the editable story source rather than injected HTML. Spreadsheet requests and custom saves require anti-forgery tokens; the custom save verifies that the submitted DevExpress document ID matches the editing session. Workbook signatures are validated before opening, and spreadsheet preview HTML is regenerated from embedded workbook bytes rather than trusted from the publication file.
