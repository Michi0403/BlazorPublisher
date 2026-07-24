# PublisherStudio repository contract

These rules are part of the source architecture. They apply to human and AI contributors.

## Stable architectural roots

PublisherStudio uses these existing solution roots and their subnamespaces:

- `Components` for Blazor frontend state, display and UI coordination
- `Controllers` for request/response entry points; Controllers start the backend for normal HTTP and WebSocket requests
- `Hubs` for persistent connection entry points and connection-specific coordination
- `Services` for reusable application capabilities, general data processing, persistence and technical I/O such as files, network communication, FFmpeg, devices and operating-system APIs
- `HostedServices` for application-lifetime scheduling, polling and start/stop lifecycle adapters
- `Domain` and `Models` for authoritative documents, shared contracts and view models

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

Every semantic contract has exactly one authoritative declaration. Shared request, event, state and result types used across Components, Controllers, Hubs, Services or HostedServices belong to the existing `Domain` or `Models` owner and must be reused directly. Do not redeclare a same-named Service-local copy of a Domain/Models type.

A separate transport DTO is allowed only at a real serialization, process or provider boundary. Name it according to that boundary (`Request`, `Response`, `Dto`, `Message` or provider-specific name), map it once at the boundary, and do not leak it through the in-process service graph. An in-process facade is not a reason to clone a shared contract.

`GlobalUsings*.cs` creates one project-wide symbol scope. Before adding or moving a public/internal type into a globally imported namespace, search all type declarations for the same simple name. The architecture tests must remain free of Domain/Models-to-Services shadow types and global-using name collisions. When moving a contract, remove the old declaration in the same change and update all consumers; do not leave compatibility duplicates behind.

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
Components -------> Services / service use cases -------> Domain / Models
Controllers ------> Services / service use cases -------> Domain / Models
Hubs -------------> Services / service use cases -------> Domain / Models
HostedServices ---> Services / service use cases -------> Domain / Models
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

Adapters belong under the owning `Services/<Area>/Import` or `Services/<Area>/Export` subnamespace. Interchange parsers and writers belong under the owning reusable Service namespace, for example `Services/PictureStudio/Import` or `Services/Publication/Import`. Their shared result and issue contracts belong in `Domain` or `Models`; Components only choose a file, display the report and commit the validated canonical result.

Open specifications do not automatically permit adding an implementation package. Prefer open specifications and existing BCL capabilities. Do not add a NuGet, npm package, native binary or separate process for a format adapter without explicit approval. DTD processing must be prohibited for SVG/XML imports. They must also reject entity expansion, executable content, event attributes and undeclared online dependencies. Package imports must validate archive paths, entry sizes and required manifest/content files. Add deterministic fixtures and architecture tests for every new adapter.

## Before adding or moving code

1. Inspect the closest existing implementation.
2. Follow its root, subnamespace and dependency direction.
3. Reuse existing Services where they fit.
4. Keep public behavior and serialized formats compatible unless the task explicitly changes them.
5. Add or update architecture and behavior tests.
6. Do not create a new architectural dialect to mirror a tutorial or library sample.

## Frontend composition, Z-order and gesture modes

Studio editors are modal editing surfaces inside the existing PublisherStudio frontend. They must not silently become alternative page composers.

- Applying a Studio result to an existing publication object must preserve that object's `Id`, `X`, `Y`, `Width`, `Height`, `Rotation`, `ZIndex`, `GroupId`, connector endpoints, animation bindings and interaction bindings unless the user explicitly edits that property.
- A media sequence, cutline, temporal section or frame/picture region is canonical content inside the owning media or picture element. Do not represent internal clip sections as extra publication siblings and do not consume new page Z-index values for editor overlays.
- Selection polygons, cutlines, playheads, handles and mode hints are local editor overlays. They must live in an editor-local stacking context, use pointer events only in the explicit mode that owns them, and never mutate Mainframe layer order.
- Every pointer-intensive Studio must expose mutually exclusive, visible mouse/touch modes. A gesture may have exactly one owner. The active mode must be visible in the ribbon/status/context UI and must reset predictably on Escape, apply, cancel and disposal.
- Pointer capture and global listeners must be released on `pointerup`, `pointercancel`, `lostpointercapture`, browser blur, modal close and component disposal. Never add an anonymous window/document listener that cannot be removed.
- Keyboard shortcuts must be scoped to the active Studio root, must ignore inputs, textareas, selects and editable content, and must be removed when the Studio closes.
- Components coordinate UI state and return canonical Domain result contracts. The Mainframe applies those results to the existing/new publication element through its established orchestration; Studio Components must not directly add sibling publication elements or bypass `EditorStateService`.
- Frontend code must not call Controllers for an in-process Interactive Server workflow when an existing reusable Service or service use case is the correct boundary.
- Every persisted visual edit must be covered in Mainframe preview, print/PDF, raster/SVG export, interactive HTML and standalone HTML where that media type is supported. A Studio-only preview is not a completed feature.
- Temporal and spatial edits are different contracts. Audio owns time only. Video may own time plus normalized frame-space polygons. Picture Studio may own document-space polygons. Never copy a two-dimensional mode into Audio or store pixel coordinates where normalized/source-independent coordinates are required.
- Studio insertion or replacement returns canonical content to the existing Mainframe command. Do not recreate an existing publication element to apply edited media, and do not bypass its established insertion placement or selection workflow.
- When a persisted media sequence changes, release removed segment assets and register the surviving/current segments. Never reuse a preview asset under a canonical media identifier unless its source and MIME type are the same canonical segment.

Before changing frontend composition or pointer ownership, inspect page Z-order normalization, selection ownership, connector geometry, preview/export projection and disposal paths. Add executable contract checks for new gesture modes and serialized fields.
