# Validation status

The v0.7 source tree was checked without restoring proprietary packages.

## Completed checks

- JSON configuration files parse successfully.
- Project files are valid XML.
- JavaScript passes `node --check`.
- C# files and Razor `@code` blocks parse without syntax errors through a Tree-sitter C# grammar; raw-string-aware delimiter scans also pass.
- Direct `@page.` Razor identifier collisions are absent.
- The dependency scan finds only `DevExpress.Blazor` and `DevExpress.Blazor.RichEdit`, both constrained to `25.2.*`.
- Connector references are normalized during publication load; broken/self-referencing connectors are discarded.
- Export clones remove guides, selection handles, connector ports/endpoints/hit areas, crop overlays, and temporary connector previews.
- No GIMP, Inkscape, or Blazor.Diagrams source/package/binary/asset is included.
- No `bin`, `obj`, `.vs`, compiled assemblies, symbols, DevExpress binaries, license keys, databases, or AI/Ollama/EF/WinUI dependencies are included.
- The final ZIP is tested with the system archive verifier and accompanied by a SHA-256 checksum.
- DevExpress data-visual markup uses explicit typed field lambdas for generic chart/pie/polar series to reduce Razor generic-inference ambiguity.
- JSON and delimited-text parsing bounds visual projections to finite row counts; live document-object sources are derived from the in-memory publication rather than executable expressions.

## Browser raster test limitation

A headless Chromium smoke test was attempted, but the container's Chromium process could not complete because its desktop/DBus environment is unavailable. PNG export therefore has code-level validation only here. The implementation now uses three rasterization paths in order: `createImageBitmap`, SVG object URL, and SVG data URL.

## Authoritative local validation

A real `dotnet restore` and `dotnet build` could not be completed because this environment has no installed .NET SDK and no access to the licensed DevExpress package feed. Run:

```powershell
dotnet restore PublisherStudio.sln
dotnet build PublisherStudio.sln -c Debug
dotnet run --project src/PublisherStudio.Web
```

Recommended smoke tests:

1. Open an older v0.2 publication and edit a story; verify font, font size, Page Layout, and DOCX/RTF/TXT/HTML downloads.
2. Import a transparent PNG over a colored shape; verify alpha, tint overlay, full recolor, and PNG export.
3. Set the page background to Transparent and export PNG; verify page alpha. Export JPEG and verify its white background.
4. Draw an arrow connector between two objects, move/resize/rotate the objects, then reconnect either endpoint.
5. Resize and collapse both side panes and verify the rulers and canvas consume the remaining workspace.

When reporting a compiler failure, include the first compiler error and affected source line; later Razor errors are commonly cascading diagnostics.


## v0.3.2 targeted validation

- The custom story modal now uses z-index 1040, below the DevExpress popup baseline of 1050.
- The existing z-index 10000 declaration is absent.
- The StoryEditor Razor component contains the field catalogue and retains the existing Quick Fields and download handlers.
- No package references or host wiring changed.


## v0.4 targeted validation

- The publication format version is 1.5; older documents remain loadable because custom path fields have defaults.
- Custom WordArt paths are stored as normalized points rather than executable SVG markup.
- Path points are clamped to the 1000 × 300 WordArt view box and limited to 32 points.
- Freehand input is simplified and limited in JavaScript before one undoable model update is committed.
- Canvas, print, SVG, website, and raster exports all use the same `WordArtView` component.
- JavaScript passes `node --check`; JSON/XML and delimiter scans pass.
- No package references or host/installer wiring changed.

## v0.4.1 targeted validation

- No attributed literal `<text>` tag remains in any `.razor` file.
- `WordArtView.razor` uses `SvgWordArtText` for straight and path-following text, including shadow, extrusion, fallback face, and gradient face layers.
- The helper escapes text through `RenderTreeBuilder.AddContent` and stores no raw SVG markup.
- No package, document format, host, service, controller, or InstallerConsole changes were made.


## v0.5 targeted validation

- The publication format version is `1.6`; `PictureSource` is optional, so older image frames remain compatible.
- Picture Studio is registered as a scoped state service and a singleton serializer/normalizer inside the existing ASP.NET Core host. No controller, route, InstallerConsole, or runtime-host replacement was introduced.
- The final raster and editable layer source are stored separately: `DataUrl` remains the normal publication image while `PictureSource` retains the non-destructive layer model.
- PNG export uses an alpha-enabled canvas; JPEG export forces a white background.
- Canvas-to-Blazor apply uses `IJSStreamReference` rather than returning a large base64 JavaScript string.
- The Picture Studio canvas is rebound after every modal reopen so direct manipulation does not remain attached to a removed DOM element.
- Existing imported pictures are initialized from their natural pixel dimensions, scaled down only when a side exceeds the 8192-pixel document limit.
- JavaScript passes `node --check`; project JSON/XML and CSS/C# lexical scans pass.
- Literal attributed SVG `<text>` tags remain absent from Razor files.
- No package was added; only the two DevExpress 25.2 package references remain.
- A Chromium Canvas smoke test was attempted, but the available headless Chromium process again stalled in the container's missing DBus/desktop environment; browser rendering still requires the local Visual Studio/browser smoke test.


## v0.6 targeted validation

- The publication format version is `1.7`; `DataObjects` defaults to an empty list, so older publications remain loadable.
- Reusable data objects support JSON, comma/semicolon/tab/pipe-delimited text, and live current-page/all-page object projections.
- The document-object source exposes only predefined publication metadata fields and evaluates no user code or query expression.
- Data visual elements are normal publication layers and participate in selection, transform, ordering, serialization, duplication, print rendering, and the existing export DOM.
- DevExpress components used by the shared renderer are `DxChart`, `DxPieChart`, `DxPolarChart`, `DxSparkline`, `DxBarGauge`, `DxGrid`, and `DxProgressBar`.
- Cartesian subtype selection maps to DevExpress common-series types; pie/doughnut uses `InnerDiameter`; polar series and pie series can show point labels.
- The hidden print tree remains mounted off-screen instead of `display:none` so DevExpress visual components can initialize before print/export cloning.
- No map or external GIS component was added because that would require an external tile/provider dependency or API key.
- No package was added; only the two DevExpress 25.2 package references remain.
- JavaScript, JSON/XML, C# syntax trees, Razor `@code` blocks, literal SVG/Razor collisions, package boundaries, and archive contents are checked.

## v0.7 targeted validation

- Picture Studio string parameters use explicit Razor expressions; no literal `_pictureEditorInitialRaster` or `_pictureEditorInitialName` component attribute remains.
- Raster decoding accepts embedded `data:image/...` and local `blob:` sources only; a failed decode is shown once and the rejected cache entry is discarded.
- The Picture Studio renderer catches layer and frame failures and reports them through Blazor instead of leaving unhandled promise rejections in the browser console.
- Picture Studio uses `DxRibbon` with Home, Insert, Draw, Effects, Render Tools, Paint Tools, and Picture Tools tabs.
- Paint layers and strokes are bounded, normalized, polymorphically serialized, and included in the existing undo/redo and export paths.
- Brush, Pencil, Line, Eraser, and Eyedropper pointer paths are handled by the existing Canvas 2D module; no JavaScript or image-processing dependency was added.
- JavaScript passes `node --check`; JSON/XML, source delimiters, direct Razor `@page.` collisions, package boundaries, and archive contents are checked.
- A real `dotnet restore`/`build` remains unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.
