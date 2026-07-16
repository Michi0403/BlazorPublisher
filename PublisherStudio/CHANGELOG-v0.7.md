# BlazorPublisher v0.7 Picture Studio drawing and recovery

## Image initialization and render recovery

- Fixed the Picture Studio component invocation so `InitialRasterDataUrl` and `InitialName` are passed as Blazor expressions instead of the literal strings `_pictureEditorInitialRaster` and `_pictureEditorInitialName`.
- Rejects malformed image sources before creating a raster layer and starts a clean editable canvas instead.
- Failed raster layers now render one visible broken-image placeholder and one status message instead of producing an endless stream of unhandled image-decoding exceptions.
- Failed image promises are removed from the browser cache so replacing the image can recover normally.
- Added render-failure/recovery callbacks between the Canvas renderer and Blazor.
- Added an embedded data-URI favicon to remove the unrelated `favicon.ico` 404.

## DevExpress Picture Studio ribbon

- Replaced the flat Picture Studio command strip with a DevExpress Ribbon.
- Added Home, Insert, Draw, and Effects tabs.
- Added contextual Render Tools, Paint Tools, and Picture Tools tabs.
- Render layers expose Clouds, Noise, Stripes, Vignette, and random-seed commands directly in the Ribbon.
- Effects expose brightness, contrast, saturation, soften/blur, grayscale, sepia, invert, and reset commands.
- Kept the existing property inspector for precise numeric control.

## Drawing tools and paint layers

- Added editable Paint layers to the Picture Studio document model.
- Added Brush, Pencil, straight Line, Eraser, and Eyedropper tools.
- Added drawing color, width, opacity, and brush-hardness controls.
- Added live stroke previews and grid-snapped line endpoints.
- Brush strokes use a soft-edge hardness model; pencil strokes remain narrow and hard edged.
- Erasing uses transparent compositing inside the selected paint layer and does not flatten other layers.
- The eyedropper samples the clean composed image without selection handles or grid lines and returns to Brush mode.
- Paint strokes participate in visibility, locking, layer order, blend modes, non-destructive effects, undo/redo, serialization, PNG/JPEG output, and insertion back into the publication.

## Compatibility and architecture

- Increased the internal Picture Studio document format from `1.0` to `1.1`; older Picture Studio documents load with no paint layers.
- Kept the publication format, ASP.NET Core/Interactive Blazor host, controllers, services, InstallerConsole, Story Editor, publication canvas, and DevExpress data visuals intact.
- Added no package. The project still references only `DevExpress.Blazor` and `DevExpress.Blazor.RichEdit` `25.2.*`.
