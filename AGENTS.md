# PublisherStudio repository contract

These rules are part of the source architecture. They apply to human and AI contributors.

## Stable architectural roots

PublisherStudio uses these existing solution roots and their subnamespaces:

- `Components` for Blazor frontend state, display and UI coordination
- `Controllers` for request/response entry points; Controllers start the backend for normal HTTP and WebSocket requests
- `Hubs` for persistent connection entry points and connection-specific coordination
- `Services` for reusable application capabilities, general data processing, persistence and technical I/O such as files, network communication, FFmpeg, devices and operating-system APIs
- `HostedServices` for application-lifetime scheduling, polling and start/stop lifecycle adapters
- `BusinessObjects` for authoritative documents, shared contracts, configuration data and view models

There is no separate `Backend` architectural root. Controllers and Hubs are backend entry points; backend work behind those entry points is implemented as reusable Services.

Do not introduce competing top-level application patterns such as `Backend`, `Endpoints`, `Features`, `Handlers`, `Commands`, `Queries`, `UseCases`, `Infrastructure` or `Application` unless Michael explicitly approves an architecture change.

## Shared service rule

Services are reusable by Components, Controllers, Hubs and HostedServices. Frontends may inject Services directly when no HTTP boundary is needed in the Interactive Server monolith.

Services must not depend on Components, Controllers, Hubs or HostedServices. Keep reusable work in Services and keep the callers thin:

- Controllers own model binding, HTTP results, authorization decisions and WebSocket negotiation.
- Hubs own persistent-connection entry and connection lifecycle.
- HostedServices own scheduling and application lifetime, then call Services for the actual work.
- Components own user interaction and UI state, then call Services or service use cases.

If logic is useful from more than one caller, it belongs in Services rather than being copied into a Controller, Hub, HostedService or Component.

## Contract and type ownership

Every semantic contract has exactly one authoritative declaration. Shared request, event, state and result types used across Components, Controllers, Hubs, Services or HostedServices belong to `PublisherStudio.BusinessObjects` and must be reused directly. Do not redeclare a same-named Service-local copy of a BusinessObjects type.

A separate transport DTO is allowed only at a real serialization, process or provider boundary. Name it according to that boundary (`Request`, `Response`, `Dto`, `Message` or provider-specific name), map it once at the boundary, and do not leak it through the in-process service graph. An in-process facade is not a reason to clone a shared contract.

`GlobalUsings*.cs` creates one project-wide symbol scope. Before adding or moving a public/internal type into a globally imported namespace, search all type declarations for the same simple name. The architecture tests must remain free of BusinessObjects-to-Services shadow types and global-using name collisions. When moving a contract, remove the old declaration in the same change and update all consumers; do not leave compatibility duplicates behind.

## Compiler-visible namespace safety

C# namespace lookup is part of the architecture. A subnamespace name can shadow a framework type used by sibling namespaces—for example, `PublisherStudio.Services.Streaming.Encoding` can shadow `System.Text.Encoding` inside `PublisherStudio.Services.Streaming.Chat` or `.Lan`. Do not create new namespace leaves whose simple names collide with framework or project types visible from the same enclosing namespace. Prefer a more precise capability name when introducing a new area.

When an existing namespace collision must remain for compatibility, every affected framework reference must use a deliberate file-level alias or `global::` qualification. Prefer an alias such as `using TextEncoding = global::System.Text.Encoding;` when the type is used more than once or inside interpolated strings. Do not rely on `using System.*` to win name resolution. In particular, the existing Streaming `Encoding` area requires the `TextEncoding` alias (or another explicit alias) in sibling Streaming namespaces.

The colon in `global::` conflicts with the format-specifier grammar of an interpolation hole when it appears directly after `{`. Never write `$"...{global::System.Text.Encoding...}"`; C# parses `global` as the expression and the first colon as formatting, producing `CS0103` and a broken format string. Use a file-level alias, compute the value in a local variable before interpolation, or—only for a one-off expression—parenthesize the alias-qualified expression as `$"...{(global::System.Text.Encoding...)}"`. Repository tests must reject an unparenthesized `{global::` interpolation.

Moving a Service, HostedService, Hub, Controller or shared contract must update the composition root and its namespace imports in the same change. `Program.cs` and `*ServiceCollectionExtensions.cs` are compile-time wiring and must not depend on accidental global-usings or stale IDE state. A DI registration of a project type must be visible through the current namespace, an explicit `using`, a global using, or a fully qualified name.

Before delivery, run a real `dotnet build` whenever the required SDK and licensed package feed are available. When they are unavailable, say so and run the repository's C# compilation-safety, architecture and contract tests; lexical delimiter checks alone are not a substitute for compilation. Never claim a compiler-clean result without a compiler run.

## Use-case orchestration

Large controller or service areas may use a `UseCases` subnamespace beneath the existing owning root. This is the approved way to stop controllers and services becoming monolithic.

Allowed examples:

- `Controllers/Streaming/UseCases`
- `Services/Streaming/UseCases`
- `Services/PictureStudio/UseCases`
- `Services/VideoStudio/UseCases`
- `Services/AudioStudio/UseCases`

`UseCases` must never become a new top-level root. A use case coordinates existing capabilities and process order. Technical parsing, storage, provider, FFmpeg, device, protocol and operating-system work remains in the relevant Service subnamespace.

## Dependency direction

The intended direction is:

```text
Components -------> Services / service use cases -------> BusinessObjects
Controllers ------> Services / service use cases -------> BusinessObjects
Hubs -------------> Services / service use cases -------> BusinessObjects
HostedServices ---> Services / service use cases -------> BusinessObjects
```

A composition-root helper beside `Program.cs` may register all roots in dependency injection. It is wiring, not business processing.

## Monolith first

PublisherStudio is an Interactive Blazor Server desktop/local-network monolith. Keep a capability inside the monolith unless a real process, deployment, crash-isolation, scaling or incompatible-dependency boundary requires a separate program. Do not introduce a microservice or microfrontend merely because a framework example uses one.

## HTTP, WebSocket and protocol routes

Main application HTTP and WebSocket routes belong to MVC controllers under `Controllers` or connection entry classes under `Hubs`. Do not add main-host `MapGet`, `MapPost`, `MapPut`, `MapDelete` or a `*Endpoints.cs` aggregation.

A private protocol listener created by a Service, such as the isolated LAN playback host, may expose only its own transport routes. It must not become a second business/application architecture and its reusable processing still belongs in Services.

## Streaming security

Provider tokens, OAuth sessions, stream keys, LAN secrets, recording destinations and machine-specific streaming configuration must not be stored in publications, templates or interchange exports. Keep them in the existing protected local stores.

## Interchange formats

The native PublisherStudio project model remains authoritative. External formats are adapters:

```text
external file -> parser -> temporary canonical model -> validation/loss report -> commit
canonical model -> capability analysis -> mapping -> external writer
```

Imports must not mutate the active project before validation succeeds. Exporters must report unsupported, flattened and lossy features. Do not reshape the native model around a third-party format.

Adapters belong under the owning `Services/<Area>/Import` or `Services/<Area>/Export` subnamespace. Interchange parsers and writers belong under the owning reusable Service namespace, for example `Services/PictureStudio/Import` or `Services/Publication/Import`. Their shared result and issue contracts belong in `BusinessObjects`; Components only choose a file, display the report and commit the validated canonical result.

Open specifications do not automatically permit adding an implementation package. Prefer open specifications and existing BCL capabilities. Do not add a NuGet, npm package, native binary or separate process for a format adapter without explicit approval. DTD processing must be prohibited for SVG/XML imports. They must also reject entity expansion, executable content, event attributes and undeclared online dependencies. Package imports must validate archive paths, entry sizes and required manifest/content files. Add deterministic fixtures and architecture tests for every new adapter.

## Frontend gesture overlays and Z-order safety

A gesture may have exactly one owner at a time. Native media controls, a Studio interaction overlay and the Mainframe must never process the same pointer sequence. Mouse/touch modes must be explicit in the ribbon or local toolbar, visually identifiable, keyboard scoped to the active Studio root, and returned to a safe default on commit, cancel, selection change or disposal. Keyboard shortcuts must be scoped to the active Studio root and ignored while the user is typing in an input, textarea, select or content-editable control.

Editor interaction overlays are transient frontend projections, not canonical content. Playheads, cutlines, range shades, crosshairs, region masks, nodes and selection guides must never be added to publication pages, picture layer collections, media segment collections or Mainframe Z-order. Keep them inside a local positioned stacking context owned by the Studio surface. Do not use an application-wide Z-index to solve a local editor problem.

An inactive overlay must use `pointer-events: none`. An active overlay may capture input only for its declared mode and must block the embedded player/control beneath it. Every window/document listener, `ResizeObserver`, object URL, pointer capture and DOM listener must be removed on rebind or disposal. High-frequency pointer movement stays in browser JavaScript/CSS; Blazor Server receives committed points or bounded state changes, not every move event.

Video region overlays must align to the actual rendered source-frame rectangle after `object-fit`, including letterboxing and arbitrary source dimensions. Persist video region points as normalized source coordinates. Picture selections use document coordinates and may contain arbitrary angled polygons. Audio remains one-dimensional and must not receive a spatial region overlay.

A media sequence, cutline, temporal section or frame/picture region is canonical content inside the owning media or picture element. Editing that content must never mutate Mainframe layer order, element identity, position, dimensions, rotation, grouping, connectors, animations or interactions. The Mainframe remains the only owner of publication insertion/update orchestration. Every persisted visual edit must be covered in Mainframe preview, print/PDF, raster/SVG export, interactive HTML and standalone HTML.

Video source-time selection belongs to the currently selected media segment and is transient until an explicit command commits it. Keep timestamp/range fields, preview overlay and sequence projection synchronized, but do not silently rewrite segment trim while the user is only selecting. A dropped compatible video may use the selection as an insertion boundary; a point means one exact source timestamp and a range means a bounded choice projected into the canonical sequence.

Range controls must tolerate zero-length and sub-step media during recording finalization. Never call `Math.Clamp(value, min, max)` unless `min <= max` is guaranteed after duration normalization. Range-selector minimum spans must be derived from the actual finite duration rather than a fixed value that can exceed a short clip.

## Before adding or moving code

1. Inspect the closest existing implementation.
2. Follow its root, subnamespace and dependency direction.
3. Reuse existing Services where they fit.
4. Keep public behavior and serialized formats compatible unless the task explicitly changes them.
5. Add or update architecture and behavior tests.
6. Do not create a new architectural dialect to mirror a tutorial or library sample.

## Interface-first services, lifetimes and local API access

Reusable behavior must be called through a public interface. Register it with an intentional Singleton, Scoped or Transient lifetime in the composition root. Stateful editor/session data is not a singleton merely for convenience; stateless catalogs, formatters and immutable adapters may be singleton. Keep compatibility by adding interfaces beside working concrete services and migrate callers incrementally.

Do not hide reusable processing in private static methods. Extract it to an injected service when another component, controller, hosted service, LocalGPT client, plugin or test could use it. Static methods remain allowed for extension methods, framework entry points, constants, compiler-generated regex and genuinely irreducible language helpers. A service must not become stateful simply to avoid parameter passing.

When an external/local automation caller can reasonably use a new service capability, expose a thin controller method that calls the same interface as the frontend. Do not duplicate logic in the controller. MVC inspection and route metadata belong in a controller-layer adapter, never inside reusable Services.

## OpenSCAD builder compatibility

OpenSCAD work must use the canonical `OpenScadDocument`/`OpenScadNode` graph, catalog definitions and registered `IOpenScadNodeRenderer` implementations. Do not introduce a closed switch-only generator or a second visual-builder model. New primitives, transforms and exporters must be registrable without rewriting the document service. Animation targets stable node IDs and must declare native/HTML export limitations.

## Release evidence

Incomplete release work must be recorded in the maintained release notes or validation report with Closed, Partial or Deferred status. A later release closes an item only when implementation evidence and an applicable maintained validation exist. Do not invent placeholder ledgers or reference deleted test suites.

## Interaction, stacking, input and frontend-failure release gate

Every release that adds or changes a visual object, toolbar, overlay, editor mode, embedded web runtime or media interaction must document the applicable checklist in its maintained release notes or validation report. Do not mark an item complete until the relevant maintained validation passes.

The checklist must cover, where applicable:

- canonical object-layer participation in Mainframe and the owning Studio;
- selection persistence, move, resize, rotate, duplicate, delete and the four layer-order operations;
- mouse, pen, touch, keyboard and controller/gamepad command routing through shared Services rather than separate behavior forks;
- local stacking contexts for toolbars, menus, hit surfaces and transient overlays, with no arbitrary application-wide maximum Z-index;
- preview, HTML/website, raster/SVG, print/PDF and video-render behavior or an explicit capability/loss marker;
- listener, pointer-capture, observer, object-URL and JavaScript interop cleanup on cancellation and disposal;
- structured `ILogger<T>` diagnostics in every new Service and every changed frontend failure boundary;
- a user-facing notification through `IUserNotificationService` for recoverable frontend failures, while expected circuit disconnects remain debug-level diagnostics rather than alarming notifications;
- a regression test for every crash, race, unreachable command or stacking defect fixed by the release.

A visual feature is not release-complete merely because its renderer looks correct. It is complete only when it remains operable through the shared object-layer structure and the supported input families, and when failures do not tear down the Blazor circuit.

## Application localization and installer independence

PublisherStudio application localization is owned by `IFileLocalizationService`, the flat JSON catalogs under `src/PublisherStudio.Web/Localization`, request-culture middleware, and the application layout selector. New application strings must use the same catalog structure and keep the reviewed culture catalogs synchronized. Application language and publication-language metadata are separate settings.

The installer console remains a dependency-light bootstrap application. Do not move the web localization service, DevExpress packages, Blazor components, or application JSON translation runtime into the installer. Installer messages stay concise and operational; the language selector belongs to PublisherStudio.Web.

## LocalGPT-aligned installer and release deployment

PublisherStudio uses the maintained LocalGPT deployment contract with PublisherStudio names and without LocalGPT-only Ollama or learning-base actions.

- The one canonical product root is `%LOCALAPPDATA%\PublisherStudio`.
- Never route PublisherStudio through `%LOCALAPPDATA%\Programs`, the former `%LOCALAPPDATA%\BlazorPublisher` root, or a second compatibility root.
- Application and setup release ZIPs retain their runtime wrapper directories, such as `winx64` and `setupwinx64`, and both archives extract into the same product root.
- Maintained Desktop and Start Menu shortcuts are Install, Update, Start, and Folder.
- A no-argument setup run performs install/update, FFmpeg preparation, Desktop and Start Menu shortcut creation, and application start. It must remain a one-click operation.
- The maintained launcher files are exactly `Install.cmd`, `Update.cmd`, and `Start.cmd`. Shortcut provisioning adds those three actions plus a direct PublisherStudio folder entry to both Desktop and Start Menu.
- Setup may continue from a temporary copy when replacing the installed setup executable. Do not introduce a second repair executable, custom release manifest, ownership ledger, transactional deployment dialect, or whole-directory replacement workflow.
- Normal install and update extraction must not delete the product root. Whole-root deletion remains explicit through `--force-delete` only.
- Former `--*-blazorpublisher` switches may remain input aliases for old shortcuts, but all maintained files, messages, profiles, tests, and documentation use PublisherStudio names.
- `Build-Release.ps1`, publish profiles, installer guards, launch profiles, repository tests, and public documentation must enforce this same contract. A conflicting repository instruction is a defect and must be replaced, not allow-listed.

