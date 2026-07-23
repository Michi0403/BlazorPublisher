# Validation status

The v0.8 source tree was checked without restoring proprietary packages.

## Completed checks

- JSON configuration files parse successfully.
- Project files are valid XML.
- JavaScript passes `node --check`.
- C# files and Razor `@code` blocks pass raw-string-aware lexical and delimiter scans; full compilation remains a release-machine check.
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
- Canvas-to-Blazor apply transfers a PNG data URL in bounded chunks, avoiding both JS stream-reference type detection and the default SignalR message-size limit.
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

## v0.8 targeted validation

- Picture Studio right-click uses the same DevExpress `DxContextMenu` component family as the publication canvas.
- Canvas context opening first performs coordinate-aware layer hit testing; layer-list context opening selects the row before the menu is rendered.
- Non-primary pointer-down events are rejected before Picture Studio transform or drawing logic starts.
- Picture Studio clipboard copies polymorphic layer models through the existing serializer-backed clone path and pastes a new identifier with an offset.
- Raster replacement targets the originally selected unlocked raster layer and preserves its transform, fit mode, effects, and layer ordering.
- Focused-canvas keyboard commands call bounded Blazor state operations; browser defaults are suppressed only for handled commands.
- Publication context commands cover text, image, WordArt, data visual, shape, connector, selected-object, and empty-page cases without changing the page model.
- Data-visual preview context commands mutate only the editor draft until **Apply changes** is chosen.
- JavaScript passes `node --check`; project JSON/XML, source delimiters, context-menu handler references, package boundaries, and archive contents are checked.
- A real `dotnet restore`/`build` remains unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.

## v0.9 targeted validation

- The publication format version is `1.8`; older files receive empty element timelines, page-transition defaults, interaction defaults, and playback defaults.
- Animation IDs are unique per page and animation `Order` is normalized as one page-wide timeline across all element types.
- Page duplication remaps object IDs, connector endpoints, interaction targets, self-page navigation, and animation IDs while retaining timeline order.
- Object duplication/paste renews animation IDs and appends cloned steps to the current page timeline.
- Text, picture, shape, WordArt, connector, and data-visual DOM nodes carry the same animation and interaction metadata in both editor and export surfaces.
- Hidden-at-presentation-start is distinct from editor visibility, so hidden playback targets remain authorable and exportable.
- HTML playback supports page transitions, automatic/with/after/click groups, repeat, true auto-reverse cycles, visibility reset on page replay, navigation, safe URL actions, and animation replay.
- `OnPageEnter` steps remain automatic even when they appear after a click group; only With/After steps following an OnClick step join that click group.
- Non-entrance animations use forward fill so delayed transform effects do not override earlier transform animations before they begin.
- Open-URL interaction playback accepts only `http`, `https`, and `mailto` schemes.
- JavaScript passes `node --check`; C# syntax trees and Razor `@code` blocks parse without syntax errors; JSON/XML files and archive integrity are checked.
- A real `dotnet restore`/`build` remains unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.

## v1.0 targeted validation

- The publication format version is `1.9`; older files receive a ten-second page timeline and no media objects.
- Audio and video are polymorphic publication elements and follow the existing serialization, layer, selection, duplication, page-copy, undo/redo, print, and HTML-export paths.
- Imported and recorded sources are bounded before embedding. Browser recordings return to Interactive Server in 24 KB text chunks rather than binary JS stream references or one oversized interop result.
- The generated example tone is created server-side as PCM WAV, so it does not return a large data URL through JS-to-.NET interop.
- DevExpress `DxRangeSelector` is used for live trim selection and timeline viewport selection; the selected range is normalized and bounded again in the C# model.
- Animation clips preserve their visual span when repeated or auto-reversed. Explicit timeline starts are evaluated directly and retain the original trigger delay for use if the clip is returned to trigger timing.
- Media playback stops when pages change, honors trim/rate/volume/fade/loop settings, and avoids double click handling when an object has an explicit interaction.
- Recording cancellation discards pending chunks and stops active camera, screen, and microphone tracks.
- JavaScript passes `node --check`; C# syntax trees and Razor `@code` blocks parse without syntax errors; JSON/XML files, XML declarations, source delimiters, patch application, archive integrity, and package boundaries are checked.
- A real `dotnet restore`/`build` remains unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.

Recommended local media smoke tests:

1. Insert MP4/WebM and MP3/WAV files, save/reopen JSON, and verify poster/waveform, trim, volume, and timeline positions.
2. Record a short camera, screen, and microphone clip; stop from both the Studio button and the browser sharing control.
3. Drag media trim handles and repeated/auto-reversed animation clips, then undo once per operation.
4. Right-click media on the page, timeline clips/background, page thumbnails, and the Media Studio preview/range.
5. Export animated HTML and verify page-entry media, click media, fades, looping, media interactions, page changes, replay, and print fallback.

## v1.0.3 beta targeted validation

- PNG/JPEG page export uses the vendored html2canvas renderer instead of reading pixels from an SVG `foreignObject`, avoiding the tainted-canvas failure that surfaced only as a `JSException` in Interactive Server.
- Raster export freezes video at the current/poster/trim-start frame, removes live media controls, preserves visible audio cards, and creates a stored ZIP for multi-page output.
- The browser video exporter records a user-selected display/tab stream through `MediaRecorder`; capture permission and optional tab audio remain explicit browser choices.
- QR Code, Code 128, Code 39, EAN-13, UPC-A, ITF-14, and Codabar generation was smoke-tested in Chromium with the vendored libraries.
- Barcode objects are polymorphic publication elements and participate in selection, layer ordering, serialization, animations, print, SVG/HTML, and raster output.
- Picture Studio selection/fill and shape tools use bounded coordinates and create editable shape layers rather than flattening the result.
- Installer source parses without C# syntax errors and includes safe ZIP traversal checks, GitHub release/pre-release asset resolution, per-user AppData installation, generated launch commands, and Start Menu provisioning.
- `node --check`, C# syntax-tree parsing, and Razor `@code` block parsing pass. A full `dotnet build` remains unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.

## v1.0.9 targeted validation

- Publisher double-click is detected by element ID, time, and pointer distance after pointer release, so the first-click Blazor rerender cannot destroy the second-click gesture.
- No-op clicks do not commit bounds; move, resize, crop, guide, and connector operations clear the pending double-click candidate.
- Connector mode remains explicit until Done, Escape, or command toggle. Object selection no longer cancels it as a side effect.
- Connector ports render above object content and normal resize handles are hidden only while connector mode is active.
- Connector target geometry is recalculated on pointer release before the connector is committed.
- Direct media insertion reads the uploaded bytes once, inspects them through the ranged local media endpoint, then copies the inspected asset to the inserted element.
- Media Studio closes only after the parent has applied the result and copied its preview asset, preserving insertion of newly imported and recorded clips.
- Barcode enum values were checked as both numeric .NET values and string names for QR, Code 128, Code 39, EAN-13, UPC-A, ITF-14, and Codabar. QR correction and module-shape mappings were checked for every enum value.
- Media Studio, Picture Studio output, Story Editor, Barcode Studio, Publication Data, Data Visual Editor, and Timeline command surfaces use DevExpress ribbons; property forms and standard dialog confirmation buttons remain conventional controls.
- JavaScript passes `node --check`; all C# files and Razor `@code` blocks parse with the C# tree-sitter grammar; JSON/XML files parse successfully.
- A full `dotnet restore`/`build` and real browser pointer/media permission test remain unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.

## v1.0.27 targeted validation

- Story print layout is read from `word/document.xml` section properties (`w:pgSz` and `w:pgMar`) and converted from twips to millimetres. Explicit portrait/landscape orientation is reconciled with stored width and height.
- Gutter placement supports normal left gutters, right-to-left gutters, and `w:gutterAtTop`; malformed optional settings do not discard otherwise valid page geometry.
- Story print HTML emits an explicit physical `@page` size and document margins, uses a same-sized preview sheet, and removes the preview-only padding before print to avoid applying margins twice.
- Transparent document backgrounds resolve to a white physical sheet; explicit document colors retain the fixed full-page print fill and paragraph/text fill materialization.
- Page dimensions and margin pairs are bounded so corrupt DOCX values cannot create negative content boxes or unbounded browser print CSS.
- JavaScript passes `node --check`; project JSON/XML and archive integrity are checked. A full `dotnet restore`/`build` remains unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.
## v1.0.28 targeted validation

- Compared the supplied expected PDF with the browser-generated Story PDF and confirmed that the defect consisted of two independent problems: RichEdit HTML retained a centered preview width, and Chromium inserted its own date/title plus URL/page-number header and footer.
- Story HTML normalization now discards BODY-level fixed width, maximum width, automatic centering, and page-preview spacing while retaining inherited typography and all formatting on actual document nodes.
- Pagination was exercised in Chromium with explicit fixed-size page wrappers and CSS columns. A five-page A4 test repeated the 12.7 mm top and left document margins on every page without cumulative horizontal drift.
- Exact Story PDF generation was exercised in Chromium for A4 portrait, A4 landscape, and a custom 148 x 210 mm page. `pdfinfo` reported the expected physical MediaBox for every generated PDF.
- Rendered PDF pages were visually inspected after generation: page background colors reach every paper edge, document content begins at the DOCX margin, paragraph/text fills retain the full content width, and no browser date, title, URL, or page number is present.
- The generated multi-page image PDF opened in Chromium's native PDF viewer and rendered all five pages. The PDF is intentionally visual/raster output so the native viewer can print exactly what is previewed without HTML print decorations.
- JavaScript passes `node --check`; generated preview scripts, project JSON/XML, source archive integrity, and C# syntax trees are checked. A full `dotnet restore`/`build` remains unavailable in this environment because the .NET SDK and licensed DevExpress feed are not installed.


## v1.0.29 Spreadsheet Studio validation

- Confirmed publication format marker `1.29` and polymorphic Spreadsheet element registration.
- Confirmed insert, blank-create, drag/drop, double-click edit, apply, cancel-new-frame, download, ribbon, context-menu, inspector, layer, thumbnail, print, and export wiring by source inspection.
- Confirmed each editor session receives a unique DevExpress document ID and that custom saves reject a client state whose document ID does not match the session.
- Confirmed internal Spreadsheet requests and custom saves send the ASP.NET Core anti-forgery request header and the Spreadsheet controller validates it.
- Confirmed XLSX/XLSM ZIP package validation, XLS compound-file signature validation, blank XLSX generation, active-sheet preview parsing, CSV/TSV preview parsing, HTML encoding, and whitelist-generated cell styles.
- Confirmed active-cell commit and begin/end synchronization handlers gate the custom client-state save path.
- Confirmed startup timeout/error handling prevents a missing licensed browser asset from leaving the Blazor modal indefinitely in its loading state.
- Confirmed local client asset paths, npm preparation script, MSBuild preparation target, release-script preflight, and source-package exclusions are mutually consistent.
- JavaScript files and the asset preparation module pass `node --check`; JSON and project XML parse; C# files pass tree-sitter syntax parsing.
- A full `dotnet restore`/`dotnet build`, DevExpress control initialization, formula calculation, and real browser save cycle remain unavailable in this environment because the .NET SDK and licensed DevExpress NuGet/npm feeds are not installed. Those tests must be run on the licensed build machine before publishing binaries.


## v1.0.30 Spreadsheet asset-build validation

- Confirmed ordinary project builds no longer contain an unconditional `npm install` execution path.
- Confirmed `SpreadsheetEditorResult` is emitted from a namespace-level `.cs` file and referenced consistently by both Razor components, eliminating the reported CS0246 failures.
- Confirmed the committed npm lockfile contains only the prebuilt `devextreme-dist`, Spreadsheet, and jQuery packages, and the supported restore path uses `npm ci --legacy-peer-deps`; the unused DevExtreme peer dependency tree that introduced `lodash.isequal` is not installed.
- Confirmed the opt-in MSBuild preparation target is disabled by default, skipped during design-time builds, and runs only while the generated Spreadsheet script is missing.
- Confirmed normal builds produce an actionable warning rather than MSB3073/9009 when the browser assets have not yet been prepared.
- Confirmed publish remains blocked when the generated offline Spreadsheet script is absent.
- Confirmed the shared PowerShell preparation script searches PATH, NVM for Windows, Program Files, the 32-bit Program Files folder, and the per-user Node.js folder; enforces Node.js 20+; runs npm restoration; invokes the existing Node copy module; and verifies all required JS/CSS outputs.
- Confirmed `Build-Release.ps1` delegates to the same preparation path and no longer invokes bare `node` or `npm` commands.
- Confirmed project XML, package JSON, command scripts, and archive paths parse structurally. A licensed end-to-end DevExpress build still needs to be executed on the user's build machine.

## v1.0.31 Spreadsheet hibernation startup validation

- Confirmed `Directory.Exists` and `Directory.CreateDirectory` execute before `AddDevExpressControls`.
- Confirmed DevExpress receives the same prepared absolute path through `hibernation.StoragePath`.
- Confirmed existing hibernation data is left intact.
- Confirmed package metadata and source archive versioning use `1.0.31`.
- A licensed end-to-end DevExpress runtime test still needs to be executed on the user's build machine.

## v1.0.32 Spreadsheet toolbar import validation

- Confirmed the Spreadsheet Home ribbon receives the custom `Open workbook` command through `SpreadsheetBuilder.Ribbon` and `SpreadsheetRibbonItemCollectionBuilder.AddButton`.
- Confirmed `OnCustomCommandExecuted` invokes only the PublisherStudio workbook picker command and leaves normal Spreadsheet commands untouched.
- Confirmed supported extension filtering, anti-forgery header forwarding, same-origin credentials, and session existence checks. The temporary 64 MB limit present in v1.0.32 was removed in v1.0.33; current source uses no PublisherStudio-defined workbook upload ceiling.
- Confirmed imported bytes pass the existing workbook validation path before replacing the session.
- Confirmed session replacement regenerates the DevExpress document ID, updates filename/format/content/preview/active sheet under the session lock, and does not modify the publication frame until Apply is selected.
- Confirmed the outer Blazor modal receives opening, ready, save, and failure messages from the same-origin iframe, disables conflicting commands during reload, and clears reload timeouts on success or failure.
- Confirmed the existing Spreadsheet drag-and-drop behavior was not removed or overridden.
- Confirmed JavaScript syntax, C# syntax trees, Razor code blocks, JSON, project XML, package version `1.0.32`, and ZIP integrity. A licensed end-to-end DevExpress build and browser test remain required on the user's development machine.

## v1.0.34 text/spreadsheet display and cursor validation

- Confirmed text-frame content sizing is reachable from the canvas context menu, Text Box Tools ribbon, and Properties panel, all updating the same `PublicationContentFitMode` field.
- Confirmed the optional spreadsheet worksheet-name badge is enabled by default, persisted per object, and controlled from Spreadsheet Tools, the context menu, and Properties.
- Confirmed the badge is not emitted when disabled and remains editor-only for print and presentation export.
- Confirmed the custom Spreadsheet **All controls** tab is inserted at index zero and the previous custom Open command is no longer injected into the standard Home tab.
- Verified the resize-cursor mapping against CSS screen-coordinate axes for 0°, 45°, 90°, and 135° rotations; horizontal, vertical, and both diagonal cursors now correspond to the actual handle movement vector.
- JavaScript passes `node --check`; JSON/XML/package metadata and ZIP integrity are validated. A licensed end-to-end DevExpress build and interactive pointer test remain required on the user's development machine because the .NET SDK and licensed DevExpress feed are unavailable here.



## v1.0.35 visualization and web-data validation

- Confirmed the Cartesian enum and JavaScript mapper cover all 23 DevExtreme Chart series types in 25.2.8, including required dual-value range fields and OHLC financial fields.
- Confirmed the visual catalogue includes pie/doughnut, all documented PolarChart types, all Sparkline types, bar/circular/linear gauges, range selector, Sankey, funnel/pyramid, tree map, data grid, and KPI; all kinds are reachable from the main ribbon, canvas insertion menu, visual editor, object context menu, and Data Tools quick-type menu.
- Confirmed special range, bubble, financial, Sankey, and tree-map field mappings are persisted, normalized for older publications, cloned by the editor, and included in standalone client configuration.
- Confirmed web bindings support monolith-relative and external absolute URLs, GET/POST/PUT/PATCH/DELETE, headers/body, JSON/XML/delimited parsing, JSON paths, refresh-on-open, manual/periodic refresh, webhook snapshots, and snapshot fallback.
- Confirmed `0` request timeout maps to `Timeout.InfiniteTimeSpan`, Kestrel/form request limits remain unbounded by PublisherStudio, file imports use `OpenReadStream(long.MaxValue)`, and response bodies are read without an application byte ceiling.
- Confirmed per-binding semaphores serialize concurrent refreshes. The live registry reuses unchanged immutable non-object data snapshots during unrelated canvas updates, resolves current-page object data against the selected page, and unregisters when a document is replaced or its scoped editor state is disposed.
- Confirmed standalone export secrets are omitted unless the user enables live HTML fetch. Enabled bindings receive a random tokenized rows route; only that route has the permissive export CORS policy.
- Confirmed website and video export force a server refresh before cloning/capture, the exported HTML embeds its snapshot and local visualization runtime, records the current loopback origin as a reconnect default with query-string override precedence, and each video page refreshes its visual DOM before recording.
- Confirmed Spreadsheet Studio presents the custom far-left tab as **Home**, renames later built-in Home labels to **All controls**, and registers begin-synchronization only once.
- Confirmed duplicate delimited-data headers are normalized once without the accidental second HashSet insertion loop.
- `node --check` passes for both JavaScript files; package JSON and project XML parse; C# files and Razor `@code` blocks pass tree-sitter syntax parsing. A complete licensed `dotnet restore`/`dotnet build` and interactive DevExpress browser test remain required on the user's release machine because this environment lacks the .NET SDK and licensed DevExpress feed.

## v1.0.36 automatic parsing and spreadsheet-data validation

1. Create a Web data object using `https://jsonplaceholder.typicode.com/todos`, response `Auto`, and no JSON path. Fetch it and verify the preview contains `userId`, `id`, `title`, and `completed` without first pressing Parse / refresh preview.
2. Save and reopen the data object. Verify all four fields and all 200 rows remain parsed.
3. Change the response format or JSON path while a snapshot exists and verify the preview reparses automatically.
4. Test an encoded JSON string, an object containing `items: [...]`, and an object with one arbitrary array property. Verify each becomes rows rather than a single blob column.
5. Open the data-visual editor and verify text, Boolean, date, and number fields all appear in the Data fields picker. Select `completed` and verify true/false plot as 1/0; select `title` and verify non-empty rows plot as 1.
6. Open Spreadsheet Studio, select a range containing headers and data, and choose Create data object from the command bar, Start ribbon, and outer footer. Verify all three entry points open the review dialog.
7. Disable the header checkbox and verify `Column 1`, `Column 2`, and so on appear as placeholders while the editable names remain required. Verify blank or duplicate names prevent creation.
8. Create the object, close Spreadsheet Studio, and insert a chart or data table. Verify the new object is listed and its workbook/sheet/range source reference appears in the data manager.
9. Verify the spreadsheet ribbon shows Start and does not contain two tabs named Home. When the native label can be reached, verify it reads All.

Static checks completed for the v1.0.36 source package:

- `node --check` passes for `spreadsheetEditorInterop.js`, `liveDataInterop.js`, and the JavaScript extracted from the Spreadsheet Razor view.
- A mocked DevExpress selection test confirms a selected `A1:D5` range is trimmed to `A1:D3`, preserves all four fields, and serializes Boolean values as text for the publication data snapshot.
- Browser-runtime parser tests confirm normal arrays, encoded JSON strings, nested/common wrappers, dotted-field flattening, and case-insensitive JSON paths produce row objects. Number, Boolean, Text, and DateTime field conversions match the server-side data-visual rules.
- Modified C# files and Razor `@code` blocks pass tree-sitter C# syntax parsing; package JSON and project XML parse successfully.
- A complete `dotnet restore`/`dotnet build` and interactive licensed DevExpress browser run remain required on the release machine because this environment does not contain the .NET SDK or the licensed DevExpress NuGet feed.


## v1.0.37 DevExtreme runtime-license and HTML export validation

Static checks completed for the v1.0.37 source package:

- Confirmed every non-modular DevExtreme document registers `vendor/devextreme-license.js` immediately after `dx.all.js`: the main application, Spreadsheet Studio, and standalone HTML export.
- Confirmed the website exporter fetches the generated runtime-license source and version marker, rejects a missing, malformed, or version-mismatched file, escapes embedded closing script tags, and inlines the license before live-data or presentation initialization.
- Confirmed `Prepare-DevExpressAssets.ps1` resolves Node.js/npm/npx, enforces Node.js 20+, reads the pinned `devextreme-dist` version, restores local packages, and invokes `devextreme-license --non-modular` from the matching `devextreme` package.
- Confirmed the preparation script never reads, prints, stores, or embeds a private key itself; key discovery is delegated to the official CLI through the registered build identity or `DevExpress_License`.
- Confirmed the Node preparation module rejects a missing, empty, or malformed generated runtime-license file and writes JSON metadata plus a plain version marker only after successful validation.
- Confirmed project publish and release packaging fail when the runtime-license script, metadata, or exact-version marker is absent; MSBuild also rejects a marker that differs from the pinned DevExpress version.
- Confirmed package metadata is `1.0.37`, DevExtreme remains pinned to `25.2.8`, and the publication document format intentionally remains `1.36`.
- JavaScript syntax, JSON, XML, Node preparation behavior with a synthetic public runtime file, script-order assertions, and ZIP integrity are checked in this environment. The PowerShell flow was reviewed structurally but could not be executed here because PowerShell is unavailable. Generation of a real runtime key and a licensed end-to-end browser run must be completed on the user's licensed build machine because this environment does not have the user's DevExpress license or the .NET SDK/feed.

## v1.0.38 timeline playback and video-cut validation

- Confirmed every publication timeline playback call receives a monotonically increasing run identifier from Blazor and includes it in playhead callbacks.
- Confirmed Blazor ignores callbacks whose run identifier is not the currently active run, including callbacks already in flight when Pause, Stop, restart, or disposal occurs.
- Confirmed animation-frame callbacks compare the exact state object stored for the page. A stale callback from a replaced run cannot schedule another frame, pause media, stop a newer run, or report completion.
- Confirmed playhead notifications permit only one JS-to-.NET invocation at a time and collapse queued progress to the latest position while retaining the final completion update.
- Confirmed timeline disposal stops page playback before releasing the module.
- Confirmed timeline pointer coordinates are clamped to the visible track, CSS positions remain finite, and media/animation commits reject non-finite start/duration values.
- Confirmed Media Studio range and numeric inputs reject non-finite values while the existing trim-start, trim-end, playback-rate, fade, loop, and source-duration calculations remain unchanged.
- `npm run test:timeline` passes stale-callback, replacement, backpressure, completion, and bounded-media-trim regression cases.
- JavaScript syntax, JSON, project XML, package version `1.0.38`, and archive integrity are validated in this environment. A full licensed DevExpress browser run and .NET build remain required on the user's development machine because this environment does not contain the .NET SDK or licensed DevExpress NuGet feed.

## v1.0.39 browser-component, REST/OData, and site-export validation

- Confirmed the publication model, insert ribbon, Component Studio, renderer, print surface, file normalization, and editor state all recognize the new `devExtremeComponent` element type.
- Confirmed all exposed component plugins (`dxDataGrid`, `dxTreeList`, `dxScheduler`, `dxForm`, editor controls, Gallery, Tile View, Menu, Context Menu, Tab Panel, Multi View, Splitter, Scroll View, Pivot Grid, and Button) are present in the bundled DevExtreme 25.2.8 `dx.all.js` file.
- Confirmed REST uses a DevExtreme `CustomStore` with client/raw and remote/processed modes, load headers/body/path, CRUD URLs/methods, encoded key suffixes, and browser credentials.
- Confirmed OData uses a DevExtreme `ODataStore`, supports versions 2/3/4 and key types, and uses the configured store for form submission when no explicit submission URL overrides it. Forms update keyed rows when update is enabled and otherwise insert only when insertion is enabled.
- Confirmed publication datasets, static snapshots, lookup datasets, web polling/webhook snapshots, standalone editor initial values, form validation, selected-row actions, and typed nested-panel data are represented in the common client contract.
- Confirmed document-wide components retain per-page geometry, synchronize configuration, resolve smart targets to the current page, populate newly added pages, and remove linked copies when returned to page scope. Page/component duplication remaps action targets to duplicated objects.
- Confirmed menu generation uses stable page IDs and both HTML runtimes expose the common page-navigation API.
- Confirmed the existing interactive-presentation export remains available and the new website export uses hash routes and browser history.
- Confirmed both exports use one shared HTML builder and embed jQuery, `dx.all.js`, the generated public runtime license, live-data runtime, and component runtime exactly once in the required order.
- Confirmed cloned component hosts are cleared before runtime initialization, avoiding duplicate DevExtreme markup in exported HTML.
- `npm run test:timeline` passes the existing lifecycle/backpressure/trim regression suite.
- `npm run test:components` passes component-catalogue, bundled-plugin, REST/OData probe, smart-action, synchronized-scope, and single-file-export contract assertions.
- JavaScript syntax, JSON metadata, project XML, C# syntax, Razor `@code` syntax, archive integrity, and generated-license exclusion are validated in this environment.
- A full licensed `dotnet restore`/`dotnet build` and interactive DevExpress browser test remain required on the release machine because this environment does not contain the .NET SDK or the licensed DevExpress NuGet feed/runtime identity.

## v1.0.40 map, viewport, CSS, and studio-layout validation

- Confirmed Map and Vector Map are present in the persisted enum, insert ribbon, Component Studio catalogue, runtime plugin map, client contract, print surface, and single-file export asset list.
- Confirmed the application and both HTML export modes load the bundled World, Europe, Eurasia, Africa, USA, and Canada DevExtreme vector-map data scripts after `dx.all.js`.
- Confirmed direct REST/OData and live publication-data rows are materialized before the initial Map/Vector Map render and on polling refresh. Map refresh updates marker and route options; Vector Map refresh updates its layers.
- Confirmed the Vector Map editor can create marker, line, and polygon features from preview clicks, edit exact longitude/latitude values, style each feature, undo points, remove/clear drawings, and import supported GeoJSON geometries.
- Confirmed Text/Docx, Spreadsheet, Map, and Vector Map store content X/Y offset and scale, expose position/reset commands, suppress object resize handles while positioning, commit through editor state for undo/redo, and preserve the transform in print and HTML output.
- Confirmed content positioning composes with the existing fit/fill/stretch transform rather than replacing it.
- Confirmed custom CSS class tokens and inline declarations are normalized; selector braces and script-like CSS constructs are rejected before preview/export.
- Confirmed all major studios use viewport-bounded width/height and no longer retain desktop minimum sizes on narrow screens.
- `node --check` passes for `componentRuntime.js` and `publisherInterop.js`.
- `node tests/componentRuntime.test.mjs` and `node tests/timelineInterop.test.mjs` pass.
- Package JSON, package lock, web project, installer project, release notes, and document format markers are aligned to `1.0.40` / `1.38`.
- A complete `dotnet restore`/`dotnet build` and licensed interactive DevExpress browser run remain required because this execution environment does not provide the .NET SDK or licensed DevExpress build identity.


## v1.0.41 SVG path and offline signal validation

- `node --check` passes for `publisherInterop.js` and `pictureStudioInterop.js`.
- Existing timeline and DevExtreme component-runtime tests pass.
- `tests/signalRuntime.test.mjs` verifies the signal domain contract, endpoint and export attributes, embedded offline runtime, synthetic-event recursion guard, finite video loops, chained video duration calculation, stable spreadsheet cell selectors, SVG output, and precise path editing controls.
- Package JSON, package lock, web project, and installer project are aligned to `1.0.41`.
- Publication format is `1.39`; Picture Studio format is `1.2`.
- Full .NET compilation requires the .NET SDK and licensed DevExpress build environment and is not available in the packaging environment used for this revision.

## v1.0.42 preview reset, signal resize, and trigger-monitoring validation

- `node --check` passes for `publisherInterop.js`.
- Existing timeline and DevExtreme component-runtime tests pass.
- `tests/signalRuntime.test.mjs` verifies the new width/height resize fields and editor controls, Stop-preview reset hook, dynamic trigger monitoring, self-contained offline runtime, and existing SVG/path/spreadsheet contracts.
- A headless Chromium runtime test applies signal translation, scale, rotation, opacity, width resize, height resize, and a CSS-class completion action; it then verifies that `reset()` restores the exact original style, class, dimensions, and opacity.
- The same browser test inserts the trigger source after runtime initialization and confirms delegated click monitoring still starts the signal.
- A second headless Chromium test verifies both automatic cleanup and an early **Stop preview** restore the exact initial opacity, inline style, and class after a filled entrance animation.
- Animation preview now snapshots and restores the affected page/object/media state, including opacity and transform values after filled Web Animations are cancelled.
- Package JSON, package lock, web project, and installer project are aligned to `1.0.42`.
- Publication format is `1.40`; Picture Studio format remains `1.2`.
- Full .NET compilation requires the .NET SDK and licensed DevExpress build environment and is not available in the packaging environment used for this revision.

## v1.0.43 Menu navigation, schema selection, and runtime-loading validation

- `componentRuntime.js` passes Node syntax validation.
- Component contract tests execute the Menu ItemClick path for stable page IDs, external URLs, and no-action rows, and also verify navigation fallback, horizontal/vertical orientation, selectable source-property fields, built-in page/document/object datasets, and the offline export runtime contract.
- `App.razor` unregisters `CommonResources.DevExtremeJS` from the DevExpress Blazor resource manager while retaining PublisherStudio's single pinned manual `dx.all.js` include, preventing a duplicate DevExtreme bundle.
- Component-specific behavior predicates are limited to controls that consume each setting in the browser runtime.
- Package JSON, package lock, web project, and installer project are aligned to `1.0.43`.
- Publication format is `1.41`; Picture Studio format remains `1.2`.
- JSON and project XML parsing and final ZIP integrity are checked during packaging.
- A full .NET/DevExpress compile and application run remain release-machine checks because this packaging environment has no .NET SDK or licensed DevExpress NuGet feed.

## v1.0.44 Menu, date-normalization, component-drag, and connector-route validation

- `node --check` passes for `componentRuntime.js` and `publisherInterop.js`.
- The Node component contract suite exercises manual Menu rendering configuration, internal page navigation, external URLs, no-action items, vertical orientation, built-in data schemas, offline export wiring, custom connector ports, advanced connector callbacks, and module exports.
- Timeline and offline Signal Connector/SVG/path regression suites pass unchanged.
- Source inspection verifies manual Menu/Context Menu configurations bypass missing-dataset blocking, while data-driven variants retain the missing-data guard.
- Static, REST, and live rows use the same date normalization, including fields discovered through `columnKinds`; invalid dates are replaced with `null`, and invalid Scheduler start rows are removed.
- Canvas interaction commits DevExtreme selection only after a native click or completed move, avoiding a Blazor rerender during the drag threshold.
- Connector custom-port IDs, object-relative coordinates, endpoint references, Bézier controls, route commits, reset behavior, print attributes, and file normalization are present in the persisted contract.
- Package JSON, package lock, web project, and installer project are aligned to `1.0.44`; publication format is `1.42`.
- JSON and project XML parsing, source delimiter checks, and final ZIP integrity are run during packaging.
- A full .NET/DevExpress compile and interactive licensed application run remain release-machine checks because this environment has no .NET SDK or licensed DevExpress NuGet feed. Headless Chromium was attempted but the available browser process is restricted and did not complete, so no browser-runtime claim is made for this revision.


## v1.0.45 vertical Menu and component-layout validation

- `node --check` passes for `componentRuntime.js`.
- The component contract suite verifies normalized orientation, shared `ResizeObserver`/`updateDimensions()` handling, nested panel refresh hooks, and the vertical Menu CSS override.
- The vertical Menu rule specifically overrides DevExtreme's `height:100%` item wrappers with natural row heights and removes the vertical centering pseudo-element.
- A headless Chromium layout check using the exact bundled DevExtreme rule and the release CSS measured the broken rows at `0/400/800px` before the override and `0/34/68px` after it.
- The fix is contained in application CSS collected by both single-file HTML exporters, while the shared component runtime is embedded in both exports.
- Timeline and offline Signal Connector/SVG/path regression suites pass unchanged.
- Package JSON, package lock, web project, and installer project are aligned to `1.0.45`; publication format remains `1.42`.
- JSON and project XML parsing, CSS delimiter checks, and final ZIP integrity are run during packaging.
- A full .NET/DevExpress compile and interactive licensed application run remain release-machine checks because this environment has no .NET SDK or licensed DevExpress NuGet feed.


## v1.0.46 media-data and chat validation

- `npm test` covers the component catalog, media data/runtime contract, platform-isolated Chat mapping, bridge send path, menu/navigation regressions, timeline lifecycle, and offline signal runtime.
- `node --check` passes for the shared component runtime and Publisher export runtime.
- The Node VM runtime test instantiates the shared Chat path with a DevExtreme-compatible plugin stub, confirms Twitch rows exclude YouTube and untagged rows, verifies outgoing bridge metadata, and checks incoming-message deduplication.
- JSON files and project XML parse successfully; application/package/installer versions are aligned to `1.0.46`, and publication format is `1.43`.
- The final ZIP is checked with `unzip -t`.
- A complete .NET/DevExpress build requires the local .NET SDK and licensed DevExpress package feed and is not claimed by this environment.

## v1.0.47 streaming validation

- Streaming runtime contract tests cover the publication model, live-source insertion, provider output requests, recording variants, browser capture ingest, audio mixing, output context, Windows hotkey contract, FFmpeg orchestration, LAN server, and Now Playing metadata.
- The Media Host LAN server exposes an explicit-IP, token-protected browser WebM path and VLC-compatible HLS path; path traversal and session mismatch requests are rejected by the source implementation.
- All project JavaScript files pass `node --check`, and all Node runtime suites pass.
- JSON files and project XML parse successfully; application/package/installer/Media Host versions are aligned to `1.0.47`, and publication format is `1.44`.
- A complete .NET/DevExpress compilation still requires a build environment with the .NET 10 SDK and the licensed DevExpress package feed.

## v1.0.48 multi-output streaming validation

- Every project JavaScript file passes `node --check`.
- All Node runtime suites pass, including component, signal, timeline, and streaming contracts.
- The streaming suite verifies the clean-base/per-output compositor contract, provider-specific Chat bridge, output recording selection, encrypted Chat-secret storage, Twitch and YouTube adapter source paths, native capture registry, Windows process-loopback activation, WebRTC signaling, HLS, and RTSP delivery contracts.
- C# source files pass raw-string-aware lexical and delimiter scans, an adjacent duplicate-declaration scan, and targeted Chat-adapter method-count checks; these caught and removed a duplicated Chat-adapter disposal declaration during the review.
- Project XML and JSON files parse successfully; application/package/installer/Media Host versions are aligned to `1.0.48`, and publication format is `1.45`.
- FFmpeg 7.1 is available in the packaging environment for command/codec inspection, and all archive entries pass the final ZIP integrity test.
- A complete .NET/DevExpress compile is not claimed by this environment because it does not contain the .NET 10 SDK or licensed DevExpress package feed. Windows process-loopback audio, physical capture devices, OAuth provider accounts, and live external ingest endpoints remain target-machine integration checks.

## v1.0.49 interface and interaction validation

- Streaming Studio uses the shared DevExpress `DxRibbon`, contextual ribbon groups, SubApp title/footer structure, status strip, profile navigation, card-based property editing, and `DxContextMenu`.
- Provider profiles, publication outputs, device profiles, and hotkeys expose right-click commands without changing their existing storage or apply/save boundaries.
- Canvas double-click and context-menu events are captured before nested DevExtreme, media, SVG, or custom content can consume them, then routed to the existing object-specific Blazor handlers.
- The global tooltip runtime observes dynamically rendered controls and supplies explicit or generated explanations for buttons, fields, selectors, links, tabs, menu items, and publication objects.
- `npm test` passes timeline, component runtime, signal runtime, streaming runtime, and interface workflow contract suites.
- `node --check` passes for the modified tooltip and publication-interaction JavaScript.
- Project XML and JSON files parse successfully; application/package/installer/Media Host versions are aligned to `1.0.49`.
- Publication format remains `1.45` and picture format remains `1.2`; no document migration or workflow replacement was introduced.
- A real `dotnet restore`/`dotnet build` remains unavailable in this environment because the .NET SDK and licensed DevExpress package feed are not installed.


## v1.0.50 single-process streaming, popup, and FFmpeg validation

- The solution and release script contain no standalone `PublisherStudio.MediaHost` or `PublisherStudio.StreamingRuntime` executable/project. Streaming implementation files compile as part of `PublisherStudio.Web` under `Services/StreamingRuntime`.
- Main application startup registers and maps the integrated runtime in-process, enables same-origin WebSockets, and retains application-singleton ownership of sessions, native captures, encoders, Chat, LAN delivery, and Windows global hotkeys.
- The streaming client performs direct registry/device-discovery calls and contains no `HttpClient` dependency or fixed loopback port.
- Browser capture, native-capture, ingest, Chat, WebRTC, and Now Playing URLs fall back to `window.location.origin`; the legacy machine-profile port is not used by the interface or runtime.
- Canvas event forwarding includes client, page, and screen coordinates. `PageSurface` reconstructs complete `MouseEventArgs` so DevExpress context menus receive valid page coordinates.
- The tooltip runtime derives its z-index from visible DevExpress/dialog overlays and hides before context-menu/click interaction.
- Live-source double-click and context-menu activation are wired through the existing browser/native capture path, with source-kind, audio, mute, and fitting commands retained in publication history.
- InstallerConsole and the application share compatible FFmpeg discovery locations. InstallerConsole exposes check/install/skip commands and package-manager fallbacks for Windows, macOS, and Linux.
- Installer updates remove obsolete `MediaHost` directories and `PublisherStudio.MediaHost*` files left by older releases, so the integrated runtime cannot coexist accidentally with a stale companion payload.
- Every project JavaScript file passes `node --check`; component, interface workflow, signal, streaming, and timeline Node contract suites pass.
- Package JSON and project versions are aligned to `1.0.50`; publication format remains `1.45` and picture format remains `1.2`.
- ZIP integrity and required-file checks are performed during final packaging. A full .NET/DevExpress compile remains a release-machine check because this environment has no .NET 10 SDK or licensed DevExpress package feed.

## v1.0.51 pointer ownership and tooltip validation

- The supplied standalone publication reproduces stale DevExtreme chart hover: after a chart point is hovered, moving onto the higher-z video leaves the old point and tooltip active. Replacing the embedded live-data runtime with v1.0.51 clears both immediately.
- Application help tooltips preserve a pending/active owner while the pointer crosses nested SVG, chart, and component descendants; movement within one object no longer restarts the hover delay.
- Help tooltips use the browser Popover top layer where available, with viewport clamping and an overlay-aware z-index fallback for older browsers.
- Export runtime marks data visuals, DevExtreme components, media, native controls, publication actions, and Signal Arrow click/hover sources as pointer owners. Only non-interactive Shape, WordArt, and Barcode objects become pointer-passive.
- Signal Connector/Arrow path geometry, canvas-to-client conversion, runner transforms, source/target IDs, and signal sequencing remain unchanged and are covered by the pointer-ownership contract test.
- Every project JavaScript file passes `node --check`; component, interface, pointer ownership, signal, streaming, and timeline Node contract suites pass.
- Package JSON, application, installer, and runtime capability versions are aligned to `1.0.51`; publication format remains `1.45` and picture format remains `1.2`.
- A full .NET/DevExpress compile remains a release-machine check because this environment has no .NET 10 SDK or licensed DevExpress package feed.


## v1.0.52 standalone visual tooltip validation

- The supplied `Untitled Publication (37).html` reproduces a doughnut tooltip rendered in `document.body` while the publication stage and page are transformed. The chart sector receives the correct hover, but the body-level tooltip remains in a different coordinate space.
- The live-data runtime now gives exported DevExtreme visual tooltips the matching `[data-publication-element]` as their container. The owner inherits page/stage scale, translation, rotation, and animation, and remains overflow-visible inside the clipped publication page.
- Headless Chromium verification confirms that hovering the green doughnut section creates `.dxc-tooltip` beneath the chart publication object and places its arrow beside the hovered segment at the transformed screen coordinates.
- Main-application visuals retain the default DevExtreme container because the override is gated by `.website-publication`.
- Existing hover cleanup still calls `hideTooltip`, clears series and point hover, and releases stale state when the pointer moves onto an overlapping media or component object.
- Every project JavaScript file passes `node --check`; component, interface, pointer ownership, signal, streaming, and timeline Node contract suites pass.
- Package JSON, application, installer, and runtime capability versions are aligned to `1.0.52`; publication format remains `1.45` and picture format remains `1.2`.
- A full .NET/DevExpress compile remains a release-machine check because this environment has no .NET 10 SDK or licensed DevExpress package feed.

## v1.0.53 Twitch OAuth and ingest validation

- Added source-contract coverage for Twitch Device Code Grant endpoints and parameters, `channel:read:stream_key`, optional IRC Chat scopes, Helix stream-key retrieval, `/validate`, refresh-token rotation, token revocation, and the official unauthenticated ingest list.
- Verified that the integrated maintenance service validates OAuth profiles on startup and hourly, while normal streaming/Chat startup obtains a valid access token through the same serialized refresh path.
- Verified that OAuth access/refresh tokens and the Twitch stream key use the existing ASP.NET Core Data Protection store and are never returned in public profile models.
- Verified that changing the configured Client ID clears the incompatible OAuth session, duplicating a profile never copies OAuth or manual secrets, and disconnect returns the profile to manual mode without deleting manual-compatible encrypted credentials.
- Verified endpoint normalization for both `{stream_key}` and `{streamKey}`, two-sample TCP latency measurement, lowest-reachable ordering, and Twitch Global fallback.
- Verified the Streaming Studio OAuth/activation-code/reconnect/disconnect and endpoint-selection wiring plus the browser authorization-window interop.
- Every project JavaScript file passes `node --check`; all Node contract suites pass.
- Package JSON, application, installer, and runtime capability versions are aligned to `1.0.53`; publication format remains `1.45` and picture format remains `1.2`.
- A full `dotnet restore`/`dotnet build` is not claimed in this packaging environment because the .NET 10 SDK and licensed DevExpress package feed are unavailable. Real Twitch authorization and ingest latency must be exercised with a registered Client ID on the release machine.

## v1.0.54 installer resilience validation

- Confirmed WinGet FFmpeg arguments include `--source winget`, `--disable-interactivity`, agreement acceptance, exact package selection, and silent mode.
- Confirmed each package-manager process is bounded, emits a 30-second heartbeat, is killed as a process tree when timed out, and is constrained by a 15-minute total FFmpeg provisioning budget.
- Confirmed FFmpeg success requires executable discovery plus a successful `ffmpeg -version` process exit.
- Confirmed WinGet package-directory discovery is shared by the installer and the runtime FFmpeg locator.
- Confirmed GitHub metadata lookup retries and release asset transfer retains/resumes `.part` files, applies a two-minute no-data timeout, and retries up to five times.
- Confirmed cached final ZIP files are opened and checked before reuse.
- Confirmed both release archives are downloaded and validated before `--force-delete` can remove the installed application.
- Confirmed permanent application download failure returns a non-zero setup result, while optional FFmpeg provisioning failure leaves PublisherStudio installed and usable.
- Package JSON, application, installer, and runtime capability versions are aligned to `1.0.54`; publication format remains `1.45` and picture format remains `1.2`.

## v1.0.55 system-font, OAuth-window, and streaming-stop validation

- The supplied source archive contains no `.git` directory, so commit-level Git history cannot be inspected from it. Release/changelog history from v0.2 through v1.0.54 was reviewed instead, and the repair is packaged as v1.0.55.
- The shared font catalog is offline-only, enumerates Windows/macOS/Linux system and user font locations, optionally reads Fontconfig, and parses TTF/OTF/TTC/OTC OpenType family-name records.
- WordArt and Picture Studio use an editable text input with a system-backed datalist; RichEdit sets `AllowUserInput`; Spreadsheet Studio sets `acceptCustomValue`. Manual font-family entry is therefore preserved in every repaired selector.
- Source-contract tests reject reintroduction of fixed WordArt/Picture font arrays and confirm the system catalog is wired into WordArt, Picture Studio, RichEdit, Spreadsheet Studio, and application dependency injection.
- Twitch authorization popup reservation/navigation return values are checked. Success and cancellation close the window explicitly; the exception path renders an error and contains no unconditional close in `finally`.
- The Streaming ribbon exposes separate **Stop streaming**, **Stop recording**, and **Stop session** commands. Provider output shutdown leaves recording and LAN delivery active, while recording can be stopped independently.
- Browser ingest shutdown requests final recorder data, waits for recorder stop, drains pending Blob-to-WebSocket writes, then closes sockets and media tracks.
- `node --check` passes for the modified streaming interop. `npm test` passes timeline, component, signal, system-font, streaming, interface, and installer contract suites.
- Application package, web project, installer project, and runtime capability versions are aligned to `1.0.55`; publication format remains `1.45` and picture format remains `1.2`.
- A full `dotnet restore`/`dotnet build` is not claimed in this packaging environment because no .NET SDK or licensed DevExpress package feed is installed. Real Twitch authorization, provider delivery, FFmpeg finalization, and OS-specific font enumeration remain release-machine checks.

## v1.0.56 preview, publication-settings, and map-drag validation

- Source contracts verify that Preview clears the old run, records a per-node base-transform map, prefers stable inline transforms, and supplies the captured baseline to each Web Animations call. Timeline playback uses the same inline-first baseline.
- Source contracts verify that standard view settings and zoom mark publications modified and remain serialized in publication JSON.
- Publication serialization explicitly removes only `streaming`; a protected Local Application Data store persists streaming settings per publication ID and legacy embedded settings migrate locally. Undo/redo preserves the active local streaming configuration.
- Source contracts verify that designer-mode `Map` components install a gesture shield that blocks map-provider pan/zoom events while allowing the publication canvas capture handler to own selection and frame movement. Presentation/export mode and `VectorMap` are unaffected.
- JavaScript syntax, Node contract suites, structured-file parsing, archive integrity, and checksum results are recorded in `TEST-RESULTS-v1.0.56.txt`.
- A full `dotnet restore`/`dotnet build` is not claimed in this packaging environment because no .NET SDK or licensed DevExpress package feed is installed. Runtime map-provider behavior and Windows Data Protection persistence remain release-machine checks.



## v1.0.57 uniform canvas-zoom validation

- Verify a publication containing RichEdit text, WordArt, a picture, an audio object with controls, a spreadsheet preview, a barcode, and a Professional Component at 100%, 50%, 75%, 125%, and 200% zoom. Object positions, line wrapping, relative font size, control size, and crop/fit appearance must remain proportional.
- Repeated zoom changes must not mutate publication element dimensions, text HTML, font sizes, media settings, content offsets, or export output.
- Drag, resize, snapping, selection handles, connector ports, connector paths, crop mode, and content-pan mode must continue to use the zoom-aware outer object frame.
- Page export and publication export must remain based on `PrintPublication` and must not contain the editor-only `publication-element-content` wrapper.
- Run `npm test` from `src/PublisherStudio.Web`; the suite includes `canvasZoomScaling.test.mjs`.
- JavaScript syntax, structured-file parsing, archive integrity, and checksum results are recorded in `TEST-RESULTS-v1.0.57.txt`.

## v1.0.58 explicit map mouse-mode validation

- Verify both provider Map and Vector Map at 50%, 100%, and 200% canvas zoom with and without attached connectors. In **Move map object**, dragging anywhere inside the component must move only the publication frame; the geographic center and zoom must remain unchanged.
- Switch to **Pan / zoom map content** through the canvas mouse indicator, Component Tools ribbon, and context menu. Drag and wheel gestures must affect only the selected map viewport; the object's X/Y/width/height and connected-object geometry must remain unchanged.
- Confirm the current mode is visibly named, the pointer cursor matches the owner, and switching between modes is immediate and repeatable.
- Select another object, clear selection, create a multi-selection, and switch between two maps. Content mode must end automatically, and no unselected map may continue accepting pan or zoom gestures.
- Confirm provider Map and Vector Map save manual center/zoom changes with the publication and restore them after reopen. Provider Map must disable auto-adjust after a deliberate manual viewport change.
- Confirm text and spreadsheet content positioning, picture crop, component activation, connector creation, context menus, presentation interaction, and all export paths remain unchanged.
- Run `npm test` from `src/PublisherStudio.Web`; the suite includes `mapInteractionModes.test.mjs` and the updated publication-persistence map-mode runtime proof.
- Run `node --check` for every non-vendor JavaScript and test module, parse project XML and package JSON, and verify the release ZIP with both ZIP CRC checks and SHA-256/SHA-512 checksums.
- A full `dotnet restore`/`dotnet build` is not claimed in this packaging environment because no .NET SDK or licensed DevExpress package feed is installed. Real DevExtreme provider and Vector Map pointer behavior remains a release-machine/browser acceptance check.

## v1.0.59 high-DPI workspace, provider-map, chart, and dialog validation

- At 100%, 200%, 300%, and 400% application canvas zoom on a UHD display, verify RichEdit text, ordinary text, WordArt, native audio/video controls, Spreadsheet previews, charts, and Professional Components remain sharp and retain their authored wrapping and geometry. Repeat with Edge/Chromium browser zoom changed independently.
- Confirm drag, resize, crop/content-pan, snapping, guides, connectors, object selection, and undo/redo still use the publication's zoom-aware outer object geometry. Verify PDF, image, SVG, print, website, and standalone HTML output is unchanged.
- Create a provider Map without a provider or key. Component Studio must refuse Apply, the live preview/canvas must show the local configuration placeholder, and browser network tools must show no map-provider request. Repeat after loading an older keyless Google Map publication.
- Configure Google, Google Static, and Azure with valid provider credentials on the licensed release machine. Confirm center, type, controls, markers, and routes work. For Google markers, verify the optional Map ID/advanced-marker path. Treat legacy Bing as compatibility-only and validate only with an existing entitled account.
- In Map and Vector Map content mode, use drag, wheel, touch, and the built-in zoom bar. The control must track smoothly, and publication center/zoom persistence must occur only after the gesture finishes; no mid-drag rerender may reset the thumb or viewport.
- Build charts from categorical, numeric, date/time, duplicate-category, multi-series, range, bubble, OHLC/stock, Sankey, and TreeMap datasets. Verify Auto-map, argument-axis mode, aggregation, and sorting create meaningful field roles; verify the Mapping check warns about missing numeric measures and incomplete specialized roles.
- Resize the browser and open Story/RichEdit, Spreadsheet, Data Manager, Chart Studio, Picture Studio, Media Studio, Barcode Studio, and Component Studio. Each main creative dialog should retain the same small responsive shadow gap on all four sides without clipping its footer, ribbon, text, or work area.
- Run `npm test`, `node --check` for every non-vendor JavaScript/test module, JSON/XML parsing, version/format alignment, ZIP CRC validation, and SHA-256/SHA-512 verification. Results are recorded in `TEST-RESULTS-v1.0.59.txt`.
- A full `dotnet restore`/`dotnet build` is not claimed in this packaging environment because no .NET SDK or licensed DevExpress package feed is installed. Provider credentials, real DevExtreme browser rendering, and UHD acceptance remain release-machine checks.

## v1.0.60 Gallery pointer and selectable zoom-renderer validation

- Insert a Gallery with at least five images, select it, and click the previous/next buttons and indicators repeatedly at 50%, 100%, 200%, and 400% application zoom. Every press must advance exactly one item and must not move the publication object.
- Click a Gallery navigation control while its object is not selected. The first click must select the object without accidental navigation or movement; subsequent control clicks must operate the Gallery. Blank component space must remain available for normal object dragging.
- Trigger harmless Blazor/runtime rerenders by selecting other ribbon tabs, changing non-Gallery view settings, or resizing the browser. The current Gallery item must remain selected. In website and standalone HTML export, native swipe and animated Gallery navigation must remain enabled.
- In **View > Zoom**, switch repeatedly between **Sharp CSS layout (default)** and **Compact transform**. The active option must be visibly checked, the page must update immediately, and object dimensions, line wrapping, selection geometry, connectors, crop/content offsets, and publication data must not mutate.
- Save and reopen the publication and a derived template. The selected canvas rendering mode must be restored. Invalid or older missing values must normalize to Sharp CSS layout.
- At 300-400% on UHD hardware, compare ordinary text/RichEdit/WordArt/media/spreadsheet content in Sharp CSS and Compact transform modes. Professional Components must remain stable and interactive through their transform-compatible editor path.
- Confirm PDF, image, SVG, print, website, and standalone HTML output is identical regardless of the editor rendering mode.
- Run `npm test`, `node --check` for every non-vendor JavaScript/test module, JSON/XML parsing, version/format alignment, ZIP CRC validation, and SHA-256/SHA-512 verification. Results are recorded in `TEST-RESULTS-v1.0.60.txt`.
- A full `dotnet restore`/`dotnet build` is not claimed in this packaging environment because no .NET SDK or licensed DevExpress package feed is installed. Real DevExtreme browser interaction and UHD acceptance remain release-machine checks.
