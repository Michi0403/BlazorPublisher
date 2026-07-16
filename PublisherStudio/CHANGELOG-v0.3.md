# BlazorPublisher v0.3

This update extends the existing ASP.NET Core + Interactive Blazor architecture. It does not replace the host, controllers, services, InstallerConsole, or the millimetre-based publication model.

## Canvas and workspace

- Hardened the five-column workspace so the canvas consumes all space left by the optional page and inspector panes.
- Added persistent, resizable pane widths, pane collapse commands, and reset-layout support.
- Improved DevExpress Ribbon command affordance so commands read as buttons rather than plain labels across 25.2 patch versions.
- Added an in-canvas connector tool hint and an explicit Done/Esc workflow.
- Added transparent page background commands. Transparent pages retain a real alpha channel in PNG output; JPEG remains white-backed.

## Layers

- Clarified that every publication object is a layer.
- Added direct creation, deletion, front/back ordering, visibility, locking, and selection controls to the Layers tab.
- The inspector remains resizable/collapsible instead of overlapping the ruler or canvas.

## Pictures

- Preserves imported PNG alpha throughout canvas rendering and export.
- Adds non-destructive tint overlay and full recolor modes, blend modes, tint presets, and strength control.
- Adds color-key transparency with adjustable tolerance and restoration of the original imported image.
- Keeps crop, fit/fill, masks, border, shadow, flips, rotation, opacity, brightness, contrast, saturation, hue, invert, grayscale, sepia, and blur.
- Strengthens PNG/JPEG rasterization with ImageBitmap plus object-URL and data-URL fallbacks.

## WordArt / LogoArt

- Adds a dedicated WordArt object with text, font, size, spacing, fill/gradient, outline, shadow, extrusion depth/color, and straight/arch/wave transforms.
- Adds contextual WordArt ribbon commands and inspector controls.

## RichEdit stories

- New stories are stored as Office Open XML (DOCX) rather than HTML.
- Existing HTML stories are upgraded to DOCX when first opened, which enables font family, font size, page layout, headers/footers, fields, and other Office commands.
- Uses canonical two-way document binding and explicit Ribbon/Print Layout mode.
- Adds quick fields for date, time, page, page count, publication name, page name, and story name.
- Adds update/show-code/show-result field commands.
- Adds direct DOCX, RTF, TXT, and HTML downloads plus RichEdit printing.

## Object connectors

- Adds attached straight, elbow, and curved connectors with solid, dashed, and dotted styles.
- Adds line, arrow, triangle, and diamond markers at either endpoint.
- Provides eight snap ports per object and accounts for object rotation.
- Connectors stay attached while objects move or resize.
- Either endpoint can be dragged to a different port.
- During reconnection, the line is hidden whenever no valid snap target is near the pointer. Releasing in that state restores the original connection immediately.
- Connectors participate in layers, selection, locking, JSON save/open, page duplication, thumbnails, SVG/website export, and printing.

## Dependency and license boundary

No package was added. Runtime package references remain only `DevExpress.Blazor` and `DevExpress.Blazor.RichEdit` 25.2.x. GIMP, Inkscape, and Blazor.Diagrams were used as behavioural/design references only; no source code, package, binary, or asset from those projects is included.
