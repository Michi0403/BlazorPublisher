# Validation status

The v0.4 source tree was checked without restoring proprietary packages.

## Completed checks

- JSON configuration files parse successfully.
- Project files are valid XML.
- JavaScript passes `node --check`.
- C# files and Razor `@code` blocks pass lexical delimiter scans.
- Direct `@page.` Razor identifier collisions are absent.
- The dependency scan finds only `DevExpress.Blazor` and `DevExpress.Blazor.RichEdit`, both constrained to `25.2.*`.
- Connector references are normalized during publication load; broken/self-referencing connectors are discarded.
- Export clones remove guides, selection handles, connector ports/endpoints/hit areas, crop overlays, and temporary connector previews.
- No GIMP, Inkscape, or Blazor.Diagrams source/package/binary/asset is included.
- No `bin`, `obj`, `.vs`, compiled assemblies, symbols, DevExpress binaries, license keys, databases, or AI/Ollama/EF/WinUI dependencies are included.
- The final ZIP is tested with the system archive verifier and accompanied by a SHA-256 checksum.

## Browser raster test limitation

A headless Chromium smoke test was attempted, but the container's Chromium process could not complete because its desktop/DBus environment is unavailable. PNG export therefore has code-level validation only here. The implementation now uses three rasterization paths in order: `createImageBitmap`, SVG object URL, and SVG data URL.

## Authoritative local validation

A real `dotnet restore` and `dotnet build` could not be run because this environment has neither the .NET 10 SDK nor access to the licensed DevExpress package feed. Run:

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
