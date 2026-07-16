# BlazorPublisher v0.5 Picture Studio

## New Picture Studio subsystem

- Added **Insert > Create picture** without changing the publication host, routing, controller, service, or InstallerConsole boundaries.
- Added **Picture Tools > Edit in Picture Studio** for both imported pictures and pictures previously created in Picture Studio.
- Added a separate full-screen image-composition dialog rather than overloading the publication canvas or RichEdit story editor.
- Added an editable `PictureDocument` source model stored beside the rendered PNG in `ImageFrameElement.PictureSource`.
- Increased the publication file format to `1.6`; older files load without a picture source and become editable after their first Picture Studio apply.

## Picture document and layers

- Transparent or colored canvases from 16 to 8192 pixels per side.
- Presets for square, landscape, Full HD, and A4 at 300 DPI.
- Raster, text, shape, fill, and procedural-render layers.
- Layer selection, naming, visibility, locking, opacity, blend mode, duplication, deletion, and ordering.
- Direct canvas move, corner resize, rotation handle, grid, snapping, zoom, 100%, and fit-to-window.
- Undo and redo for model changes.

## Image and design tools

- Raster contain, cover, and stretch modes.
- Horizontal/vertical flipping and color tint/recolor.
- Shared non-destructive brightness, contrast, saturation, hue, soften/blur, grayscale, sepia, and inversion controls.
- Text layers with font, size, alignment, bold, italic, fill, outline, and shadow.
- Rectangle, rounded rectangle, ellipse, and line layers with fill and stroke.
- Solid, linear-gradient, and radial-gradient fill layers.
- Procedural Clouds, Noise, Stripes, and Vignette layers with seed, colors, scale/detail, softness, contrast, angle, and stripe width.
- Normal, Multiply, Screen, Overlay, Darken, and Lighten blend modes.

## Output and publication integration

- PNG output retains alpha; JPEG output is flattened against white.
- Picture Studio can download PNG/JPEG directly or stream a PNG back into the Blazor publication without passing a large base64 result through a single JavaScript string call.
- The publication stores the final PNG for normal page rendering/export and the separate editable Picture Studio source for later edits.
- Creating a new image inserts a correctly proportioned image frame; editing an existing frame retains its page position and size.

## Ribbon visibility fix

- Added explicit enabled and disabled foreground colors for DevExpress ribbon command wrappers and descendants so primary/context commands no longer render as white text on a light background before first activation.

## Dependency boundary

No package was added. The web project still references only:

- `DevExpress.Blazor` `25.2.*`
- `DevExpress.Blazor.RichEdit` `25.2.*`

The renderer is native Canvas 2D JavaScript and the state/file models are native C#/.NET.
