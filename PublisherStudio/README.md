# PublisherStudio / BlazorPublisher foundation

PublisherStudio is a .NET 10 Interactive Blazor Server publication editor. It keeps the existing ASP.NET Core host and optional InstallerConsole architecture, while the product UI is a Publisher-style document and picture editor built with DevExpress DXperience 25.2.

## Implemented in this source package

- DevExpress Blazor `DxRibbon` with File, Home, Insert, Page Design, View, Animations, Picture Tools, Text Box Tools, WordArt Tools, Connector Tools, Shape Tools, and Data Tools tabs.
- DevExpress `DxContextMenu` on the page, selected publication objects, Picture Studio canvas/layers, and data-visual preview.
- Multi-page workspace with page thumbnails, layers, selection handles, drag, resize, rotation, ordering, alignment, duplication, copy/paste, undo, and redo.
- Canvas-linked horizontal and vertical rulers that follow page position, scroll, zoom, and the selected unit.
- Millimetre, centimetre, inch, and pixel ruler units.
- Guides created by dragging from either ruler, movable guides, guide deletion by dragging outside the page, grid display, and snap options.
- A larger preset catalogue: A3, A4, A5, Letter, Legal, Tabloid, business card, landscape variants, and square, plus custom dimensions.
- Text frames edited with DevExpress Blazor RichEdit and its Office ribbon; stories use DOCX storage, support dynamic fields, and download as DOCX, RTF, TXT, or HTML.
- Image frames with preserved PNG alpha, replacement, fit/fill, interactive crop panning, wheel-based crop zoom, picture rotation, flipping, opacity, brightness, contrast, saturation, hue, inversion, grayscale, sepia, blur, masks, borders, shadows, tint/full recolor, blend modes, color-key transparency, and frame-ratio presets.
- A separate **Picture Studio** opened by **Insert > Create picture** or **Picture Tools > Edit in Picture Studio**, with transparent canvases, direct transforms, undo/redo, a layer clipboard, keyboard shortcuts, contextual right-click commands, and editable raster, text, shape, fill, paint, and procedural-render layers.
- Publication-level reusable data objects sourced from JSON, pasted CSV/TSV/delimited text, or live publication-object metadata.
- Insertable live DevExpress data visuals: Cartesian charts, pie/doughnut charts, polar charts, sparklines, circular bar gauges, data tables, and KPI progress indicators.
- A guided data-visual editor for choosing component type, chart subtype, category, series, numeric fields, labels, legends, ranges, and row limits with a live preview.
- Picture Studio effects include tint/recolor, soften/blur, tonal and color adjustments, gradients, Clouds, Noise, Stripes, Vignette, opacity, and blend modes; its editable source is retained inside the publication while a rendered PNG is used by the page.
- Rectangles, rounded rectangles, ellipses, lines, and WordArt/LogoArt with fixed transforms plus freely drawn text paths.
- Attached straight, elbow, and curved connectors with eight ports per object, reconnectable endpoints, line styles, and arrow/triangle/diamond markers.
- Native self-contained JSON publication format (`.pubstudio.json`).
- Current-page PNG, JPEG, and browser-oriented SVG export.
- Page-wide animation timelines, object entrance/emphasis/motion/exit effects, page transitions, click triggers, playback timing, and object interactions across every publication element type.
- Embedded audio and video page objects with import, camera/screen/microphone recording, generated sample audio, non-destructive trims, fades, playback settings, and layer integration.
- An on-demand page timeline docked to the bottom of the workspace with playhead transport, a DevExpress visible-range selector, draggable/resizable animation clips, and draggable/trim-enabled media clips.
- Self-contained animated HTML presentation export with responsive scaling, keyboard/control navigation, fullscreen, replay, looping, automatic advance, and print fallback.
- Browser print workflow suitable for printing or the browser's Save as PDF command.
- Optional InstallerConsole that installs a published payload, starts the Blazor host, removes it, or downloads and publishes a source ZIP without requiring Git.

## Design reference boundary

GIMP and Inkscape were used only as behavioural references for rulers, guides, crop interaction, and non-destructive image adjustments. Blazor.Diagrams was used only as a behavioural reference for ports, snapping, routing, and endpoint reconnection. No source code, binaries, packages, or assets from those projects are included. The implementation remains native Blazor/C#/JavaScript and keeps the Apache-2.0 project boundary.

## Requirements

- Visual Studio with the .NET 10 SDK. The included `global.json` accepts .NET 10 feature bands starting at 10.0.100.
- DevExpress DXperience 25.2 packages and a configured DevExpress NuGet feed.
- A valid DevExpress license.
- A current Chromium-based browser is recommended for the raster/SVG export pipeline.

## Build and run

```powershell
dotnet restore PublisherStudio.sln
dotnet build PublisherStudio.sln -c Debug
dotnet run --project src/PublisherStudio.Web
```

Optional fixed port:

```powershell
dotnet run --project src/PublisherStudio.Web -- --port 5198
```

Without a supplied port, Kestrel asks the operating system for a loopback port and writes the resolved endpoint to:

```text
%LOCALAPPDATA%/PublisherStudio/runtime/server.json
```

## First-use workflow

1. Add or select a picture.
2. Use **Picture Tools > Crop**, then drag the picture inside its frame and use the mouse wheel to zoom.
3. Double-click the picture or choose **Finish crop** to leave crop mode.
4. Open the right-hand **Properties** tab for live picture adjustments.
5. Drag from a ruler onto the page to create a guide.
6. Choose **Insert > Connector** or **Arrow connector**, then drag from one round object port to another. Drag a selected endpoint to reconnect it; Esc stops the tool.
7. Select WordArt and choose **WordArt Tools > Draw path**. Pick a preset or click **Draw freehand**, then drag the control points to refine the baseline.
8. Choose **Insert > Create picture** to start a transparent layered image, or select an image and choose **Picture Tools > Edit in Picture Studio**. Use **Insert into publication / Apply to picture** to retain the editable layer source.
9. Choose **Insert > Manage data** to create a reusable data object from JSON, pasted CSV/TSV, or live page/object metadata.
10. Choose **Insert > Chart**, **Pie**, **Polar**, **Sparkline**, **Bar Gauge**, **Data Table**, or **KPI**, then select fields and subtypes in the live visual editor.
11. Choose **Insert > Media** to embed audio/video, or open **Create audio / Create video** for microphone, camera, screen, generated-tone, trim, fade, volume, and playback controls.
12. Select an object and open the **Animations** inspector tab or ribbon tab to add entrance, emphasis, motion, or exit steps. Open the docked timeline pane from the Animations, View, or Media Tools ribbon tabs and use it to drag animation timing and arrange or trim media clips against one page playhead.
13. Right-click the page, page thumbnails, selected objects, timeline clips/background, Picture Studio canvas/layers, Media Studio preview/range, or a data-visual preview for commands relevant to that location.
14. Use **File** for JSON save/open and PNG, JPEG, SVG, animated website, or print/PDF output.

The file picker is reset before every picture/open command, so selecting the same file again also triggers replacement.

## InstallerConsole

Publish and install the web project:

```powershell
dotnet publish src/PublisherStudio.Web -c Release -r win-x64 --self-contained false -o artifacts/payload
dotnet run --project src/PublisherStudio.InstallerConsole -- install --payload artifacts/payload --start
```

Start an existing installation:

```powershell
dotnet run --project src/PublisherStudio.InstallerConsole -- start
```

Build directly from a ZIP without Git:

```powershell
dotnet run --project src/PublisherStudio.InstallerConsole -- source --source-zip https://github.com/OWNER/REPOSITORY/archive/refs/heads/main.zip --start
```

## Deliberate limits

This is the next editor foundation, not a claim of complete Publisher/InDesign parity. Text-frame linking, master pages, full pen-tool Bézier handles, obstacle-aware connector routing, color management, CMYK/PDF-X prepress, imposition, and full packaging of external assets remain later milestones. Picture Studio now includes editable brush, pencil, line, eraser, and eyedropper tools in addition to compositing and non-destructive layers; lasso selections, pixel masks, clone/heal tools, and pressure-sensitive input remain later milestones. Data visuals currently cover self-contained publication data; external databases, authenticated APIs, maps that require external tile/GIS providers, dashboards, reports, and spreadsheet-calculation engines remain deliberate later integrations. SVG export currently uses an SVG `foreignObject` representation so it preserves the HTML text-frame rendering in Chromium; a future pure-vector exporter should translate each publication element directly to SVG primitives. The animation/media document model is exporter-neutral and animated HTML is implemented; native PowerPoint timing-tree/media output and encoded video export remain later exporter modules. Media trimming is non-destructive and browser recording formats depend on `MediaRecorder` support in the active browser.

See [`CHANGELOG-v1.0.md`](CHANGELOG-v1.0.md), [`CHANGELOG-v0.9.md`](CHANGELOG-v0.9.md), [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/ANIMATION_EXPORT.md`](docs/ANIMATION_EXPORT.md), and [`VALIDATION.md`](VALIDATION.md).

## v0.3.1 WordArt visibility hotfix

See `CHANGELOG-v0.3.1.md`.


## v0.3.2 RichEdit popup hotfix

See `CHANGELOG-v0.3.2.md`. The custom Edit Story backdrop now stays below DevExpress popup layers, so RichEdit ribbon drop-downs and dialogs remain visible and interactive.


## v0.4 custom WordArt paths

See `CHANGELOG-v0.4.md`. WordArt can follow eight presets or a freehand path with draggable points, reversible direction, text-position control, and path-distance control.

## v0.4.1 Razor SVG text hotfix

See `CHANGELOG-v0.4.1.md`. WordArt SVG text nodes are emitted by a small Blazor render component so Razor no longer mistakes SVG `<text>` elements for its reserved pseudo-element.


## v0.5 Picture Studio

See `CHANGELOG-v0.5.md`. A decoupled layered Picture Studio can create transparent raster assets from scratch, edit existing publication pictures, render procedural clouds/noise/stripes/vignettes, preserve its editable source in the publication, and return a normal PNG to the established page workflow. Ribbon command foreground colors are also stabilized.


## v0.6 publication data and DevExpress visuals

See `CHANGELOG-v0.6.md`. Publications can now store reusable data objects and insert live DevExpress Cartesian, pie/doughnut, polar, sparkline, bar-gauge, grid, and KPI visuals. Data can come from JSON, pasted delimited text, or the publication's own page/object metadata.

## v0.7 Picture Studio drawing and recovery

See `CHANGELOG-v0.7.md`. Picture Studio now uses a DevExpress Ribbon for insert, render, drawing, raster, and effect commands; adds editable paint layers with Brush, Pencil, Line, Eraser, and Eyedropper tools; and fixes the literal initial-image binding that caused repeated image decode exceptions.

## v0.8 desktop context menus

See `CHANGELOG-v0.8.md`. Picture Studio now has hit-tested canvas and layer-list context menus, an internal layer clipboard, and focused-canvas keyboard shortcuts. The publication canvas and data-visual editor context menus now expose more relevant insert, type, display, page, and editing commands.


## v0.9 animations and interactive presentation export

See `CHANGELOG-v0.9.md`. Every publication element can participate in a page-wide animation timeline, pages have independent transitions and advance rules, and objects can navigate, open links, reveal/hide other objects, or replay animations. Website export is now an animated, self-contained presentation.


## v1.0 media studios and visual page timeline

See `CHANGELOG-v1.0.md`. Audio and video are first-class publication objects, Video Studio and Audio Studio provide simple browser-native creation and non-destructive editing, and the page timeline provides a second visual way to arrange animations and media.
