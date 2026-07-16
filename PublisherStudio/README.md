# PublisherStudio / BlazorPublisher foundation

PublisherStudio is a .NET 10 Interactive Blazor Server publication editor. It keeps the existing ASP.NET Core host and optional InstallerConsole architecture, while the product UI is a Publisher-style document and picture editor built with DevExpress DXperience 25.2.

## Implemented in this source package

- DevExpress Blazor `DxRibbon` with File, Home, Insert, Page Design, View, Picture Tools, Text Box Tools, WordArt Tools, Connector Tools, and Shape Tools tabs.
- DevExpress `DxContextMenu` on the page and on selected objects.
- Multi-page workspace with page thumbnails, layers, selection handles, drag, resize, rotation, ordering, alignment, duplication, copy/paste, undo, and redo.
- Canvas-linked horizontal and vertical rulers that follow page position, scroll, zoom, and the selected unit.
- Millimetre, centimetre, inch, and pixel ruler units.
- Guides created by dragging from either ruler, movable guides, guide deletion by dragging outside the page, grid display, and snap options.
- A larger preset catalogue: A3, A4, A5, Letter, Legal, Tabloid, business card, landscape variants, and square, plus custom dimensions.
- Text frames edited with DevExpress Blazor RichEdit and its Office ribbon; stories use DOCX storage, support dynamic fields, and download as DOCX, RTF, TXT, or HTML.
- Image frames with preserved PNG alpha, replacement, fit/fill, interactive crop panning, wheel-based crop zoom, picture rotation, flipping, opacity, brightness, contrast, saturation, hue, inversion, grayscale, sepia, blur, masks, borders, shadows, tint/full recolor, blend modes, color-key transparency, and frame-ratio presets.
- Rectangles, rounded rectangles, ellipses, lines, and WordArt/LogoArt.
- Attached straight, elbow, and curved connectors with eight ports per object, reconnectable endpoints, line styles, and arrow/triangle/diamond markers.
- Native self-contained JSON publication format (`.pubstudio.json`).
- Current-page PNG, JPEG, and browser-oriented SVG export.
- Self-contained multi-page HTML website export.
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
7. Use **File** for JSON save/open and PNG, JPEG, SVG, website, or print/PDF output.

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

This is the next editor foundation, not a claim of complete Publisher/InDesign parity. Text-frame linking, master pages, free Bézier path editing, obstacle-aware connector routing, color management, CMYK/PDF-X prepress, imposition, and full packaging of external assets remain later milestones. SVG export currently uses an SVG `foreignObject` representation so it preserves the HTML text-frame rendering in Chromium; a future pure-vector exporter should translate each publication element directly to SVG primitives.

See [`CHANGELOG-v0.3.md`](CHANGELOG-v0.3.md), [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), and [`VALIDATION.md`](VALIDATION.md).

## v0.3.1 WordArt visibility hotfix

See `CHANGELOG-v0.3.1.md`.


## v0.3.2 RichEdit popup hotfix

See `CHANGELOG-v0.3.2.md`. The custom Edit Story backdrop now stays below DevExpress popup layers, so RichEdit ribbon drop-downs and dialogs remain visible and interactive.
