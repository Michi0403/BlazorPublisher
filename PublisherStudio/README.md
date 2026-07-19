# PublisherStudio

**The offline Publisher workbench that puts documents, spreadsheets, presentations, graphics, audio, and video on one canvas.**

PublisherStudio is a local-first desktop publishing environment powered by an ASP.NET Core loopback server. Build print publications, reports, interactive presentations, websites, and recorded media without sending project data to a cloud service.

- **Page layout and presentations:** layered, grouped, animated, rotatable objects with rulers, guides, snapping, transitions, print, PDF, HTML, and recorded-presentation export.
- **Word-class stories:** embedded DevExpress RichEdit documents with DOCX storage, fields, page settings, and backgrounds.
- **Excel-class workbooks:** embedded DevExpress Spreadsheet editing, import/download, and scalable worksheet frames.
- **Picture Studio:** a built-in mini image workshop for crop, filters, masks, tinting, transparency, drawing, gradients, and layered picture documents.
- **Audio and video studio:** import, record, trim, arrange, preview, and export media directly inside publications.
- **Live data publishing:** every DevExtreme Cartesian series type plus pie/doughnut, PolarChart, sparkline, bar/circular/linear gauges, range selector, Sankey, funnel, pyramid, tree map, data grid, KPI visuals, barcodes, QR codes, WordArt, connectors, and data-bound fields.

Your files remain under the signed-in user's control. PublisherStudio listens only on the local loopback interface.

> **Developer setup:** Node.js 20 or newer is required only to prepare the offline DevExpress Spreadsheet browser assets from npm. Run `Prepare-SpreadsheetAssets.cmd` after a clean checkout. Published/installed releases include those static files and do **not** require Node.js or npm at runtime.

## Implemented in this source package

- DevExpress Blazor `DxRibbon` with File, Home, Insert, Page Design, View, Animations, Picture Tools, Text Box Tools, WordArt Tools, Connector Tools, Shape Tools, and Data Tools tabs.
- DevExpress `DxContextMenu` on the page, selected publication objects, Picture Studio canvas/layers, and data-visual preview.
- Multi-page workspace with page thumbnails, layers, selection handles, drag, resize, rotation, ordering, alignment, duplication, copy/paste, undo, and redo.
- Canvas-linked horizontal and vertical rulers that follow page position, scroll, zoom, and the selected unit.
- Millimetre, centimetre, inch, and pixel ruler units.
- Guides created by dragging from either ruler, movable guides, guide deletion by dragging outside the page, grid display, and snap options.
- A larger preset catalogue: A3, A4, A5, Letter, Legal, Tabloid, business card, landscape variants, and square, plus custom dimensions.
- Text frames edited with DevExpress Blazor RichEdit and its Office ribbon; stories use DOCX storage, support dynamic fields, and download as DOCX, RTF, TXT, or HTML.
- First-class spreadsheet frames edited with the DevExpress ASP.NET Core Spreadsheet control in an application-styled modal; workbooks support XLSX, XLSM, XLS, CSV, and text import, formula editing, worksheet tabs, download, embedded publication storage, static canvas/print previews, layering, transforms, and double-click editing.
- Image frames with preserved PNG alpha, replacement, fit/fill, interactive crop panning, wheel-based crop zoom, picture rotation, flipping, opacity, brightness, contrast, saturation, hue, inversion, grayscale, sepia, blur, masks, borders, shadows, tint/full recolor, blend modes, color-key transparency, and frame-ratio presets.
- A separate **Picture Studio** opened by **Insert > Create picture** or **Picture Tools > Edit in Picture Studio**, with transparent canvases, direct transforms, undo/redo, a layer clipboard, keyboard shortcuts, contextual right-click commands, and editable raster, text, shape, fill, paint, and procedural-render layers.
- Publication-level reusable data objects sourced from imported or pasted CSV/TSV, JSON, XML, live publication-object metadata, the local PublisherStudio monolith API, configurable REST polling, or tokenized webhooks.
- Insertable live DevExpress/DevExtreme visuals: all 23 Cartesian series types (including range, bubble, stock, and candlestick), pie/doughnut, every PolarChart series, every Sparkline series, bar/circular/linear gauges, range selector, Sankey, funnel, pyramid, tree map, data grid, and KPI progress.
- A guided data-visual editor for choosing component type, chart subtype, category, grouped/multiple series, range values, bubble size, OHLC fields, Sankey targets, tree-map parents, labels, legends, gauges, and row limits with a live preview.
- Picture Studio effects include tint/recolor, soften/blur, tonal and color adjustments, gradients, Clouds, Noise, Stripes, Vignette, Bloom, Neon, Lens Flare, Grain, Motion Blur, Wind, and Ocean Waves; drawing includes brush, pencil, spray, toothbrush, lines, arrows, base forms, area selections, and solid/gradient selection fills.
- Rectangles, rounded rectangles, ellipses, lines, and WordArt/LogoArt with fixed transforms plus freely drawn text paths.
- Attached straight, elbow, and curved connectors with eight ports per object, reconnectable endpoints, line styles, and arrow/triangle/diamond markers.
- Native self-contained JSON publication format (`.pubstudio.json`).
- Per-page PNG and JPEG export (ZIP for multi-page documents), browser-oriented SVG export, and browser-capture WebM presentation export.
- Page-wide animation timelines, object entrance/emphasis/motion/exit effects, page transitions, click triggers, playback timing, and object interactions across every publication element type.
- Embedded audio and video page objects with import, camera/screen/microphone recording, generated sample audio, non-destructive trims, fades, playback settings, media animation cues, and layer integration.
- An on-demand page timeline docked to the bottom of the workspace with playhead transport, a DevExpress visible-range selector, draggable/resizable animation clips, and draggable/trim-enabled media clips.
- Self-contained animated HTML presentation export with responsive scaling, keyboard/control navigation, fullscreen, replay, looping, automatic advance, and print fallback.
- Browser print workflow suitable for printing or the browser's Save as PDF command.
- Insertable QR and linear barcodes with common symbologies, colors, readable text, module designs, and QR error correction.
- Windows InstallerConsole bootstrapper that downloads the latest GitHub release, installs per-user into AppData, starts the host, updates or uninstalls it, and creates Start Menu command entries without requiring Git.

## Design reference boundary

GIMP and Inkscape were used only as behavioural references for rulers, guides, crop interaction, and non-destructive image adjustments. Blazor.Diagrams was used only as a behavioural reference for ports, snapping, routing, and endpoint reconnection. No source code, binaries, packages, or assets from those projects are included. The implementation remains native Blazor/C#/JavaScript and keeps the Apache-2.0 project boundary.

## Requirements

- Visual Studio with the .NET 10 SDK. The included `global.json` accepts .NET 10 feature bands starting at 10.0.100.
- DevExpress DXperience 25.2 packages and a configured DevExpress NuGet feed.
- A valid DevExpress license.
- Node.js 20 or newer with npm is a **source-development/build-machine requirement only** for the one-time Spreadsheet client-asset preparation step. Normal Visual Studio builds and installed PublisherStudio releases do not invoke Node.js or npm.
- A current Chromium-based browser is recommended for the raster/SVG export pipeline.

## Build and run

```powershell
.\Prepare-SpreadsheetAssets.cmd
dotnet restore PublisherStudio.sln
dotnet build PublisherStudio.sln -c Debug
dotnet run --project src/PublisherStudio.Web
```

`Prepare-SpreadsheetAssets.cmd` is required once after extracting a clean source archive, and again only when the pinned Spreadsheet client-package versions change. It finds standard Node.js/NVM installations even when Visual Studio was opened with an older `PATH`. Close and reopen Visual Studio after installing Node.js. The preparation uses the committed lockfile, the prebuilt `devextreme-dist` package, and disabled npm peer auto-installation, so unused DevExtreme source dependencies and their deprecation warnings are not restored.

The published application serves the Spreadsheet JavaScript and CSS from its own ASP.NET Core host. No CDN or internet connection is required at runtime. A clean source checkout needs access to the DevExpress NuGet/npm packages only during restore and the one-time asset preparation step.

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
9. Choose **Insert > Spreadsheet** to import XLSX, XLSM, XLS, CSV, or text, or choose **Create spreadsheet** for a blank workbook. Edit it in Spreadsheet Studio and use **Apply to frame** to keep the workbook embedded in the publication. Double-click the frame to reopen it.
10. Choose **Insert > Manage data** to create a reusable data object from JSON, pasted CSV/TSV, live page/object metadata, the local monolith API, an external REST endpoint, or a webhook inbox.
11. Choose any visual from **Insert > Charts and data**. The Cartesian editor exposes every DevExtreme series type, while the remaining entries cover the full pie, PolarChart, Sparkline, gauge, range, Sankey, funnel, pyramid, tree-map, grid, and KPI families.
12. For live standalone HTML, enable **Allow standalone HTML to fetch this endpoint** on the data object. The export always contains the last successful snapshot; connect it to a running PublisherStudio instance with `?publisherApi=http://127.0.0.1:PORT`, or let it call a CORS-enabled external endpoint directly.
13. Choose **Insert > Media** to embed audio/video, or open **Create audio / Create video** for microphone, camera, screen, generated-tone, trim, fade, volume, and playback controls.
14. Select an object and open the **Animations** inspector tab or ribbon tab to add entrance, emphasis, motion, or exit steps. Open the docked timeline pane from the Animations, View, or Media Tools ribbon tabs and use it to drag animation timing and arrange or trim media clips against one page playhead.
15. Right-click the page, page thumbnails, selected objects, timeline clips/background, Picture Studio canvas/layers, Media Studio preview/range, or a data-visual preview for commands relevant to that location.
16. Use **Insert > Barcode / QR** for QR, Code 128, Code 39, EAN-13, UPC-A, ITF-14, or Codabar objects.
17. Use **File** for JSON save/open and PNG, JPEG, SVG, animated website, WebM presentation capture, or print/PDF output.

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

This is a substantial offline workbench, not a claim of complete Publisher/InDesign parity. Text-frame linking, master pages, full pen-tool Bézier handles, obstacle-aware connector routing, color management, CMYK/PDF-X prepress, imposition, and full packaging of external assets remain later milestones. Picture Studio now includes editable brush, pencil, line, eraser, and eyedropper tools in addition to compositing and non-destructive layers; lasso selections, pixel masks, clone/heal tools, and pressure-sensitive input remain later milestones. Data visuals support embedded files, publication-object data, local monolith APIs, configurable HTTP requests, and webhook snapshots. Database-specific adapters, OAuth credential vaults, GIS/tile providers, report designers, and the planned LAN/VLC/provider streaming workbench remain later integrations. Spreadsheet Studio is a separate first-class workbook object rather than a chart/data-visual substitute. SVG export currently uses an SVG `foreignObject` representation so it preserves the HTML text-frame rendering in Chromium; a future pure-vector exporter should translate each publication element directly to SVG primitives. The animation/media document model is exporter-neutral and animated HTML plus browser-capture WebM export are implemented; native PowerPoint timing-tree/media output and direct server-side video encoding remain later exporter modules. Media trimming is non-destructive and browser recording formats depend on `MediaRecorder` support in the active browser.

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

See `CHANGELOG-v0.6.md`. Publications can now store reusable data objects and insert live DevExpress Cartesian, pie/doughnut, polar, sparkline, bar-gauge, grid, and KPI visuals. Data can come from CSV/TSV, JSON, XML, or the publication's own page/object metadata.

## v0.7 Picture Studio drawing and recovery

See `CHANGELOG-v0.7.md`. Picture Studio now uses a DevExpress Ribbon for insert, render, drawing, raster, and effect commands; adds editable paint layers with Brush, Pencil, Line, Eraser, and Eyedropper tools; and fixes the literal initial-image binding that caused repeated image decode exceptions.

## v0.8 desktop context menus

See `CHANGELOG-v0.8.md`. Picture Studio now has hit-tested canvas and layer-list context menus, an internal layer clipboard, and focused-canvas keyboard shortcuts. The publication canvas and data-visual editor context menus now expose more relevant insert, type, display, page, and editing commands.


## v0.9 animations and interactive presentation export

See `CHANGELOG-v0.9.md`. Every publication element can participate in a page-wide animation timeline, pages have independent transitions and advance rules, and objects can navigate, open links, reveal/hide other objects, or replay animations. Website export is now an animated, self-contained presentation.


## v1.0 media studios and visual page timeline

See `CHANGELOG-v1.0.md`. Audio and video are first-class publication objects, Video Studio and Audio Studio provide simple browser-native creation and non-destructive editing, and the page timeline provides a second visual way to arrange animations and media.


## v1.0.5 beta stabilization

See `CHANGELOG-v1.0.5.md`. Presentation video recording, publisher pointer state, deterministic layer ordering, CSV/XML data import, barcode value import, and Story Editor ribbon sizing were stabilized for beta.

## v1.0.19 recording and presentation export stabilization

See `CHANGELOG-v1.0.19.md`. Browser recordings remain available as local downloadable blobs until Media Studio closes, large recordings are embedded only on demand through small JS-interop chunks, and HTML/video presentation exports share one fixed publication frame with working page transitions. Versions v1.0.12 through v1.0.18 were iterative releases without separate changelog files.

## v1.0.20 object workflow and recovery stabilization

See `CHANGELOG-v1.0.20.md`. Desktop file drag/drop now creates live-positioned picture, video, text, and Markdown objects; grouping is respected by alignment, z-order and animation; chart layering and minimum sizing are stabilized; WordArt gains a font selector; timeline dragging terminates reliably; and atomic local recovery protects work around navigation and presentation export.

## v1.0.21 interaction, story and canvas-state stabilization

See `CHANGELOG-v1.0.21.md`. Multi-object clipboard and keyboard editing now work as one coherent canvas workflow; quick inserts and desktop files display live drag previews; animation and pointer states return reliably to selection mode; RichEdit formatting is preserved in canvas, print and exported output; and chart content remains stable across z-order changes.

## v1.0.22 selection and desktop clipboard stabilization

See `CHANGELOG-v1.0.22.md`. Mouse selection is restored without removing the v1.0.21 drag/drop and keyboard workflow; selected and grouped objects move together across z-levels; and files or text copied from the desktop can be pasted at the last canvas position.

## v1.0.23 story fidelity and command-surface completion

See `CHANGELOG-v1.0.23.md`. RichEdit formatting and document backgrounds now survive apply, reopen, canvas display, print and HTML/video export; Publisher and Picture Studio context menus expose the relevant animation and layer settings; selected renderer properties are clearly identified; and all ribbon commands have built-in icons.


## v1.0.24 story print and marquee selection

See `CHANGELOG-v1.0.24.md`. Story documents containing embedded pictures can be applied through chunked interop, print output retains page and text highlight fills, and mouse rectangle selection again works across every visible z-level while respecting persistent groups.

## v1.0.25 dropped-story stability

See `CHANGELOG-v1.0.25.md`. Dropped text and Markdown now enter the publication as native OpenXML stories, legacy HTML stories upgrade only after RichEdit reports the document loaded, and mail-merge settings attach only after that same safe lifecycle point so opening a dropped story cannot terminate the Blazor circuit.

## v1.0.26 Story print-preview isolation and DOCX drop import

See `CHANGELOG-v1.0.26.md`. Story Editor printing now runs from an independent blob-backed preview window with materialized print fills, so page colors and highlights print without keeping an iframe attached to the Blazor circuit. DOCX files can be dropped directly onto the publication as editable OpenXML stories.

## v1.0.27 DOCX-aligned Story printing

See `CHANGELOG-v1.0.27.md`. Story Editor print preview, browser printing, PDF output, and standalone HTML now use the page size, orientation, margins, and gutter stored in the live DOCX, while preserving full-page background colors and text fills.

## v1.0.28 exact Story PDF preview and print

See `CHANGELOG-v1.0.28.md`. Story printing now paginates into explicit physical sheets using the live DOCX paper size and margins, removes RichEdit HTML preview-width centering, and opens an application-generated PDF instead of printing the preview HTML. This keeps the Word/LibreOffice placement and prevents browser-added date/title, URL, and page-number decorations.
## v1.0.29 Spreadsheet Studio

See `CHANGELOG-v1.0.29.md`. Spreadsheet workbooks are now first-class publication layers. The supported DevExpress ASP.NET Core Spreadsheet control runs as a same-origin MVC/Razor island inside the Blazor editor shell, with local assets, CSRF-protected document requests, session-bound saves, XLSX/XLSM/XLS/CSV/TXT import, workbook download, double-click editing, and safe static previews for canvas, print, and export.

## v1.0.30 Spreadsheet build and compiler fixes

See `CHANGELOG-v1.0.30.md`. Normal Visual Studio builds no longer execute `npm install`, so a missing or stale Node.js `PATH` cannot surface as the opaque MSBuild exit code 9009. The shared Spreadsheet result model is now compiled from a normal C# source file, fixing the `SpreadsheetEditorResult` CS0246 errors. The preparation command also avoids restoring unused peer packages that caused the `lodash.isequal` deprecation warning. Run `Prepare-SpreadsheetAssets.cmd` once from the solution root; publishing still refuses to create an incomplete package when the local Spreadsheet browser files are absent.

## v1.0.31 Spreadsheet hibernation startup fix

See `CHANGELOG-v1.0.31.md`. PublisherStudio now creates `%LOCALAPPDATA%\PublisherStudio\SpreadsheetHibernation` before DevExpress Spreadsheet hibernation is configured, preventing first-run `DirectoryNotFoundException` failures. The folder is reused when already present. Node.js remains required only for developers and release-build machines preparing the offline Spreadsheet browser assets; end-user runtime installations do not require Node.js or npm.

## v1.0.32 Spreadsheet toolbar workbook loading

See `CHANGELOG-v1.0.32.md`. Spreadsheet Studio now contains an **Open workbook** command directly on the DevExpress Home ribbon. It loads XLSX, XLSM, XLS, CSV, TXT, or TSV into the current creation/editing session, updates the workbook name, and reloads the control with a fresh document identifier while retaining the existing drag-and-drop workflow. The import remains local and CSRF-protected. Node.js is still a development/release-build requirement only and is not required on end-user systems.

## v1.0.33 workbook scale modes and rotation-aware transforms

See `CHANGELOG-v1.0.33.md`. PublisherStudio no longer imposes application-level file upload ceilings. Spreadsheet Studio gains an original Open Workbook icon, permanent workbook commands, and dedicated Publisher, Format, and Cells & Data ribbon tabs so essential commands remain directly reachable at compact editor sizes. Spreadsheet and RichEdit text objects now support Natural/Clip, Fit, Fill, and Stretch display modes. Rotated objects resize along their own axes, resize cursors follow the visual handles, connectors update during the gesture, and WordArt plus its freehand path editor follow the actual object proportions.

## v1.0.34 text-frame display parity and canvas polish

See `CHANGELOG-v1.0.34.md`. Text frames now expose Natural/Clip, Fit, Fill, and Stretch directly from their canvas context menu as well as Text Box Tools and Properties. Spreadsheet frames gain an optional worksheet-name badge, the Spreadsheet island places **All controls** at the far left without injecting another Open command into Home, and rotated resize cursors now match the real visual drag axis.

## v1.0.35 complete visualization and web-data workbench

See `CHANGELOG-v1.0.35.md`. This release adds the complete DevExtreme chart-series catalogue and publisher-focused supplemental data visuals, plus a transport-neutral web binding used by local monolith routes, configurable REST requests, and tokenized webhook inboxes. Open publications are exposed through read-only DTO endpoints rather than mutable Blazor state. Web data keeps an embedded snapshot, refreshes on open or on a configurable polling interval, and can be forced immediately before HTML/video export.

Standalone HTML remains useful with no server: it renders the embedded snapshot and carries the local DevExtreme runtime inside the single exported file. When live retrieval is explicitly enabled, JavaScript can call a CORS-enabled external API or reconnect to PublisherStudio. The generated HTML remembers the loopback origin active at export time; `?publisherApi=http://127.0.0.1:PORT` overrides it after a restart or port change. Reconnection uses a per-binding tokenized rows endpoint, including for webhook-backed data. The transport contract deliberately reserves `Stream` for the later LAN/VLC/provider streaming workbench; no streaming implementation is included yet.

Spreadsheet Studio now labels PublisherStudio's far-left workflow tab **Home** and renames the built-in, command-dense DevExpress Home tab to **All controls**, removing the ambiguous duplicate Home labels.

