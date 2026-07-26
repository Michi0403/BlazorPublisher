# PublisherStudio v1.0.85

## Interface-first architecture evolution

- Added `PublisherStudioServiceCollectionExtensions.AddPublisherStudioApplication` as the application composition root while keeping the existing monolith and working component call paths.
- The composition root now records all application-owned registrations in the architecture registry, including compatible legacy concrete registrations, so `/api/domain-context` can report the real service/lifetime surface instead of only the newly introduced contracts.
- Introduced public interfaces and explicit DI lifetime descriptors for newly touched Video Studio interchange, OpenSCAD, automation, screenshot, code editing, localization/path and render-export capabilities.
- Split reusable polygon geometry, browser runtime generation and OpenSCAD generation out of `VideoLayerInterchangeService` instead of retaining private static helpers.
- Added an MVC adapter boundary for API-surface discovery so reusable Services remain independent from Controllers and `Microsoft.AspNetCore.Mvc`.
- Added `/api/domain-context`, which reports exported business objects, service contracts, lifetimes, methods and related controller routes for LocalGPT/AICouncil integration.
- Preserved existing concrete `VideoLayerInterchangeService` injection for frontend compatibility while also exposing `IVideoLayerInterchangeService`.

## LocalGPT/AICouncil browser automation

- Added public mouse, pointer, keyboard, focus, text and value command contracts with singleton queue services and controller endpoints.
- Added screenshot request, claim, completion, status and file-download services/controllers.
- Added `automationInterop.js`, which lets an active PublisherStudio browser page execute pending same-origin DOM commands and capture selected application elements through html2canvas.
- Kept the boundary deliberately browser-local: this release does not perform operating-system-global input injection.

## OpenSCAD model graph and animation

- Replaced closed string-generation assumptions with a public `OpenScadDocument`/`OpenScadNode` graph, typed parameter definitions, validation results and renderer registrations.
- Added catalog definitions for OpenSCAD's complete native basic 3D and 2D primitive set, text, transforms, CSG operations, extrusion, projection, import, surface and custom raw nodes.
- Added catalog-backed default-node creation plus composable document code parts and `module_call` nodes, so assembled modules can be reused and animated as selected graph parts without closing the model behind a string-only generator.
- Added separate `IOpenScadNodeRenderer` implementations for primitives, child-wrapping operations and advanced/custom source.
- Added node-ID animation tracks for translation, rotation, scale, resize and alpha using `$t`, easing, bounded ranges, looping and ping-pong behavior.
- Added OpenSCAD catalog, validation, generation, formatting and Video Studio layer endpoints.
- Added API access to Video Studio polygon normalization/resampling, OpenSCAD point output, shared canvas runtime generation and Mainframe insert generation.
- Retained the Video Studio/OpenMorph-compatible blob path and HTML compatibility metadata.

## Programming-language text editing

- Added public language-profile and code-formatting interfaces with controller commands for language detection, formatting, line/block comments and token analysis.
- Added profiles for C#, JavaScript, TypeScript, HTML, XML, CSS, JSON, YAML, Markdown, SQL, Python, PowerShell, Bash, C/C++, Java/Kotlin, Rust, Go, PHP, Ruby, Swift, OpenSCAD and plain text.
- Added a Code ribbon tab and code workspace to Story Editor, including language choice, indentation, formatting, comment toggling, token summary and insertion at the RichEdit caret.
- Enabled DevExpress RichEdit spell checking through the built-in service and `CheckSpelling` property.

## Localization and configurable paths

- Added a simple file-based localization service using `Localization/<culture>.json` beside the application.
- Added starter application resources for English (US), German, Spanish and Japanese with culture fallback.
- Added DevExpress community satellite NuGet packages for German, Spanish and Japanese Blazor and RichEdit UI resources.
- Added request-culture configuration and runtime DevExtreme community-message loading.
- Added configurable default directories for images, video, audio, documents, exports, OpenSCAD and projects through `appsettings.json`, project settings, services and controller methods.

## Render export repair and extensibility

- Kept all existing PNG/JPEG/SVG export commands and added explicit Render labels for current-frame video/effect output.
- Fixed blank cloned canvases by snapshotting source canvas pixel buffers before raster/SVG conversion.
- Freezes the current video frame and accessible same-origin iframe content alongside canvas effects.
- Added an `IRenderExportCatalogService` and API describing media/effect capture, vector preservation and HTML render-before-export requirements for PNG, JPG, SVG, HTML and PDF paths.
- Existing interactive HTML export remains unchanged; native-only effects continue to be marked as requiring render before HTML export.

## Previous open-task closure

- **Closed:** v1.0.84 `Math.Hypot`, CA1859 and IDE0305 findings remain fixed and protected.
- **Closed:** temporal selection layers, selected-point ownership/deletion, 3D blob interchange and HTML compatibility marking from v1.0.83 remain protected by regression tests.
- **Closed for this release scope:** browser-local input automation, screenshots, domain context, basic OpenSCAD catalog, code-command API, configurable path service and current-frame raster render repair.

## Explicitly partial or deferred

- **Partial:** interface migration covers new/touched areas; legacy concrete services and private static candidates are not mass-rewritten. They must migrate incrementally when touched to avoid destabilizing working components.
- **Partial:** OpenSCAD transform/alpha animation is generated; generic arbitrary-parameter animation requires a node-specific renderer.
- **Deferred:** visual OpenSCAD builder UI. The node graph/catalog/renderer architecture is intentionally ready for it.
- **Deferred:** native OpenSCAD process execution, exact CGAL render, cancellation/sandboxing and direct STL/3MF/OFF/AMF/CSG/DXF/SVG/PNG production.
- **Partial:** the code workspace provides formatting/comments/token spans, but not a full token-colored IDE, LSP, semantic diagnostics or external formatter plugin host.
- **Partial:** RichEdit includes the built-in English spelling service. Additional language dictionaries require a confirmed open and redistributable dictionary source.
- **Partial:** localization infrastructure and starter resources are present; not every historic hard-coded PublisherStudio UI literal has been migrated.
- **Partial:** export capabilities are extensible through interfaces; runtime discovery of third-party exporter assemblies is deferred.
- **Deferred:** operating-system-global mouse/keyboard injection. Current commands target the active PublisherStudio browser DOM.
- **Environment validation pending:** native .NET 10/Razor/DevExpress build and native OpenSCAD rendering require Michael's licensed development machine.

The maintained status source is `docs/architecture/task-ledger.md`.
