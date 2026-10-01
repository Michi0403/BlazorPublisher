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


## PowerShell maintenance-guard contract

Build guards are production code for the repository: they must run under Windows PowerShell 5.1 and modern `pwsh`, and failure output must point to the blamed source file/line and explain architectural repair choices rather than suggest bypasses. Under `Set-StrictMode -Version Latest`, never assume a pipeline result is an array merely because several values are possible. If later code uses `.Count`, indexing, or collection-only behavior, materialize the pipeline with an outer `@(...)` or use an explicit generic collection. Likewise, do not wrap `ConvertFrom-Json` itself in `@(...)` when the JSON root may be an array; Windows PowerShell 5.1 can preserve that returned JSON array as one pipeline object, creating a nested collection shape. Assign the parsed JSON first and iterate that value explicitly.

A maintenance guard must not fail with its own incidental `PropertyNotFoundStrict`, parser, encoding, or platform error before it can diagnose application source. When adding or changing a guard, review the single-result, zero-result, and multi-result paths, keep source paths cross-platform, and make the emitted repair choices preserve architecture (DevExpress ownership, render boundaries, service ownership, localization ownership) instead of recommending removal of the protected feature.

## Blazor render-state and Razor component-expression contract

A Razor component is not one backend object that happens to render HTML. Depending on how it is reached, the same `.razor` source can participate in distinct execution environments and component instances: static SSR/prerender, an InteractiveServer circuit, an InteractiveWebAssembly client, and a child instance inheriting a parent render boundary. A routable page can also be reused as a parameterized child component. Treat those as separate entry/lifecycle paths even when they share one source file.

Never assume fields, DI scope, authorization state, browser availability, synchronization context, or lifecycle progress from one render instance survives into another. In particular, prerender and the later interactive instance are separate instances. Browser/DOM JavaScript interop must wait for successful interactive attachment (`OnAfterRenderAsync` or an equivalent maintained attachment gate), and disposal must not call browser interop for an instance that never attached. Child components inherit the parent render mode unless an explicitly reviewed boundary says otherwise; do not add or remove `@rendermode`, prerendering, or root render boundaries as a shortcut. When authorization or other state must cross from prerender into InteractiveWebAssembly, use the framework's supported persisted/cascading state path rather than relying on instance fields.

Keep Razor component attributes parser-simple. Do not put generic method invocations such as `Data="@Enum.GetValues<T>()"` or mixed literal/expression values such as `CssClass="@BaseClass extra"` directly into component attributes. Prepare render-time values in typed instance properties/fields or lifecycle state and bind the simple member. For event callbacks, when the component expects a `Task`, either bind a named async handler or use a simple callback such as `async () => await Service.MethodAsync(context)` when that preserves the required context. Do not replace a DevExpress component with native HTML merely because the Razor expression is awkward.

## InteractiveServer transient UI-state ownership

Complex browser/vendor controls own their live caret, selection, document buffer, drag handle and similar gesture state while the user is interacting. Do not two-way bind those transient values through an `InteractiveServer` circuit when the callback will immediately rerender the same control. That feedback loop can replay stale state and appears as jumping carets, toolbar values cycling through old selections, sliders snapping back, drag handles becoming uncontrollable, or vendor widgets losing focus. Scalar form fields such as normal text/value/checked editors remain ordinary model binding; this rule targets control-internal transient interaction state.

The maintained repair pattern is: initialize a complex editor one-way; observe any required live notification without allowing that notification's automatic component rerender; discard selection/caret state when an editor/document generation changes; and write durable application state only at an explicit boundary such as Apply, Save, `change`, pointer-up, or `RangeSelectorValueChangeMode.OnHandleRelease`. If continuous preview is essential, keep the immediate preview browser/vendor-owned and coalesce or commit server updates instead of feeding every intermediate gesture value back through Blazor. Never solve the race by disabling the editor, removing DevExpress, or adding arbitrary delays.

`build/Assert-TransientUiStateOwnership.ps1` and `build/audit_transient_ui_state_ownership.py` are build-breaking guards for this contract. They reject two-way live document/selection bindings on complex DevExpress editors and server-round-tripped `DxRangeSelector` handle movement. LocalGPT also rejects native range `@oninput` because its maintained slider contract is commit-on-change. PublisherStudio keeps its reviewed browser coalescer for legacy/live-preview native ranges; the guard verifies that the coalescer remains present and continues to exclude DevExpress/DevExtreme-owned controls.

## Razor layout ownership and component diagnostics contract

Every render-producing `.razor` component and routable page, including reusable subcomponents, must have a DevExpress Blazor semantic layout owner. The maintained component boundary is one containment-only `<div class="razor-component-boundary">` used to stop CSS/size bleed; the DevExpress layout sits immediately inside it and still owns the actual layout. The approved semantic layout owners are exactly `DxGridLayout`, `DxCarousel`, `DxDrawer`, `DxFormLayout`, `DxSplitter`, `DxStackLayout`, and `DxTabs`. `DxFormLayout` is the default and strongly preferred owner for editors, scanners, configuration/settings surfaces, field groups, and ordinary component forms. Use `DxStackLayout` only for a genuinely one-dimensional flow, and `DxGridLayout` only for a genuinely two-dimensional/special composition; do not use either as a mechanical replacement for `<section>` or a normal form. A Grid-to-Stack nesting is justified only when the child is a distinct one-dimensional subgroup rather than rows/columns the Grid itself should own. `App.razor` is the sole document-host layout exception because it owns the HTML document shell; a Razor file that emits no markup does not need a visual layout. Do not create broad exception lists for ordinary UI components.


The maintenance-only classes `.razor-component-boundary`, `.razor-component-layout-owner`, and `.razor-section-layout-owner` are compatibility shells, not new visual layout boxes. The loaded `wwwroot/css/site.css` block marked `DEVEXPRESS_RAZOR_LAYOUT_COMPATIBILITY` must keep the component boundary plus the generated FormLayout row/item/control shell box-neutral (`display: contents`) so pre-migration grid/flex placement, height propagation, overflow, and sizing continue to belong to the established component roots. Genuine DevExpress layouts inside the component keep their normal layout behavior. Do not remove the compatibility block, turn these maintenance wrappers into visible sizing containers, or replace DevExpress controls with native HTML to work around wrapper geometry.

`<section>` is forbidden in maintained Razor source. Replace it with the appropriate approved DevExpress layout while preserving the existing feature and behavior. `<dialog>` is also forbidden. Native/manual modal ownership is forbidden too: a native element may not own `role="dialog"`, a `modal-backdrop`/`dialog-backdrop`, focus trapping, or overlay positioning for an application window. Modal/window/prompt workflows must use `DxPopup` or another reviewed DevExpress window/prompt control; native elements may remain only as body content inside that DevExpress owner. When a modal is not required, conditional visibility may show or hide an approved DevExpress layout. Never solve a Razor/layout problem by deleting the feature or replacing DevExpress controls with simpler native HTML.

Every Razor component owns exactly one typed `@inject ILogger<ComponentName> Logger` directive. Operational component methods and code-behind methods must own method-local diagnostics boundaries and structured logging. Expected cancellation or circuit-disconnect paths may log at Debug level, but failures must not become invisible. This component-level logging requirement is additional to any global logger factory or notification boundary.

Every `DxFormLayoutItem` that uses an explicit `<Template>` must give that template a locally unique `Context` name. Do not leave the default Razor child-content name `context` on FormLayout item templates: nested DevExpress buttons, popups, combo-box item templates, or another FormLayout item can then create `RZ9999` child-content scope ambiguity. The repair is to name the template context explicitly while preserving the DevExpress hierarchy, not to remove or flatten controls.

Render-time properties are passive state projections. They may expose parameters, fields, auto-properties, already-materialized state, or small deterministic projections over that state. They must not start I/O, asynchronous work, service mutation, renderer dispatch, or blocking waits. Values that require those operations are prepared by an awaited lifecycle/event/service path and stored before rendering consumes them; do not create async properties.

`build/Assert-RazorMaintenanceArchitecture.ps1` and `build/audit_razor_maintenance_contract.py` are build-breaking architecture guards. They must report all findings from one audit with exact file/line/column locations and architectural repair choices. They enforce the containment-only root boundary, `DxFormLayout` as the normal semantic owner for form/editor UI, explicit architectural justification for a non-Form primary owner, rejection of generic StackLayout wrappers and unexplained Grid-to-Stack nesting, the `<section>`/`<dialog>` bans, the native/manual modal-owner ban, typed component loggers, and method-local logging. The async-boundary architecture guard owns the passive render-state/asynchronous-property-call rule. The DevExpress retention guard also verifies that this enforcement remains wired into `Directory.Build.targets`; removing or bypassing the guard is not an acceptable repair.

Build/architecture guards must make failures actionable. A changed guard should emit Visual-Studio/MSBuild-style `file(line,column): error CODE:` diagnostics whenever a source location exists, followed by the architectural choices that satisfy the rule. Diagnostics must describe the intended architecture, not propose disabling the guard or deleting the protected feature as the easy workaround.

## Frontend gesture overlays and Z-order safety

A gesture may have exactly one owner at a time. Native media controls, a Studio interaction overlay and the Mainframe must never process the same pointer sequence. Mouse/touch modes must be explicit in the ribbon or local toolbar, visually identifiable, keyboard scoped to the active Studio root, and returned to a safe default on commit, cancel, selection change or disposal. Keyboard shortcuts must be scoped to the active Studio root and ignored while the user is typing in an input, textarea, select or content-editable control.

Editor interaction overlays are transient frontend projections, not canonical content. Playheads, cutlines, range shades, crosshairs, region masks, nodes and selection guides must never be added to publication pages, picture layer collections, media segment collections or Mainframe Z-order. Keep them inside a local positioned stacking context owned by the Studio surface. Do not use an application-wide Z-index to solve a local editor problem.

An inactive overlay must use `pointer-events: none`. An active overlay may capture input only for its declared mode and must block the embedded player/control beneath it. Every window/document listener, `ResizeObserver`, object URL, pointer capture and DOM listener must be removed on rebind or disposal. High-frequency pointer movement stays in browser JavaScript/CSS; Blazor Server receives committed points or bounded state changes, not every move event.

Video region overlays must align to the actual rendered source-frame rectangle after `object-fit`, including letterboxing and arbitrary source dimensions. Persist video region points as normalized source coordinates. Picture selections use document coordinates and may contain arbitrary angled polygons. Audio remains one-dimensional and must not receive a spatial region overlay.

A media sequence, cutline, temporal section or frame/picture region is canonical content inside the owning media or picture element. Editing that content must never mutate Mainframe layer order, element identity, position, dimensions, rotation, grouping, connectors, animations or interactions. The Mainframe remains the only owner of publication insertion/update orchestration. Every persisted visual edit must be covered in Mainframe preview, print/PDF, raster/SVG export, interactive HTML and standalone HTML.

Video source-time selection belongs to the currently selected media segment and is transient until an explicit command commits it. Keep timestamp/range fields, preview overlay and sequence projection synchronized, but do not silently rewrite segment trim while the user is only selecting. A dropped compatible video may use the selection as an insertion boundary; a point means one exact source timestamp and a range means a bounded choice projected into the canonical sequence.

Range controls must tolerate zero-length and sub-step media during recording finalization. Never call `Math.Clamp(value, min, max)` unless `min <= max` is guaranteed after duration normalization. Range-selector minimum spans must be derived from the actual finite duration rather than a fixed value that can exceed a short clip.

## Async-boundary Components and Services

Components, service implementations, hosted services, and service interfaces must keep asynchronous work explicit, observable, and owned, but they are not required to turn every pure synchronous helper or framework-mandated callback into a fake `Task`. Synchronous in-memory projections, parsers, clone/format helpers, event adapters, required framework overrides, and genuinely synchronous native/library contracts remain valid when they do not hide asynchronous work. Do not add `Task.FromResult`, empty `DisposeAsync` methods, or other ceremony merely to satisfy a signature scanner.

Methods whose names end in `Async` must expose an awaitable contract (`Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, `IAsyncEnumerable<T>`, or `IAsyncEnumerator<T>` as appropriate), and `async void` is forbidden. Properties/getters must never start or synchronously consume asynchronous work. Render-time state that depends on I/O, mutation, or asynchronous services is prepared by an awaited lifecycle/event/service path and then read passively; a small deterministic synchronous projection over already-materialized state is allowed.

Sync-over-async remains forbidden on renderer paths and inside methods that already own an awaitable workflow. Do not use task `.Result` or `GetAwaiter().GetResult()` there; await the operation. A service/native adapter whose external contract is genuinely synchronous may bridge that synchronous boundary locally, but the bridge must not be called from renderer/asynchronous orchestration and must not be propagated upward as the preferred application API. Blocking waits such as `Task.WaitAll`, `Task.WaitAny`, `WaitOne`, `Thread.Sleep`, `Thread.Join`, and `Monitor.Wait/Enter` remain forbidden in maintained application workflows.

Short synchronous state protection is not an asynchronous architecture violation by itself. `lock`, `Interlocked`, and `Volatile` are permitted for bounded in-memory critical sections and atomic flags when no await, I/O, renderer dispatch, or long-running work occurs while the synchronization is held. Long-lived coordination still belongs to cancellation-aware awaited ownership such as `SemaphoreSlim.WaitAsync`, channels, async queues, or an existing repository service.

Do not discard asynchronous work with `_ = SomeOperationAsync()`, `_ = InvokeAsync(...)`, or `_ = Task.Run(...)`. Await work that belongs to the current operation. Work that intentionally outlives a synchronous framework/event boundary must transfer explicit ownership to `ISupervisedTaskRunner`, a hosted/background service, or a retained worker task whose cancellation and completion are owned. `_ = await SomeOperationAsync()` is not fire-and-forget; only the already-awaited result value is discarded.

Disposal follows actual resource ownership. Use `IDisposable` for synchronous cleanup and `IAsyncDisposable` when cleanup itself must await asynchronous resources or an owned worker. Do not add empty asynchronous disposal boundaries to components that own no asynchronous cleanup, and do not require a component to implement both interfaces merely for maintenance symmetry.

`build/Assert-AsyncOnlyArchitecture.ps1` remains a zero-baseline build gate, despite its compatibility-preserving filename. The audit is an async-boundary validator and must fail only contracts it can determine reliably from source: invalid `Async` signatures, `async void`, asynchronous work hidden in getters, renderer/awaitable-path sync-over-async, prohibited blocking coordination, and discarded asynchronous invocations. It must not misclassify domain members named `Result`, `_ => await ...` lambdas, `_ = await ...` result discards, pure synchronous helpers, framework-required synchronous methods, short atomic state, or components that simply do not need async disposal. There is no grandfathered finding list and no normal-build skip switch.
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

## Release task ledger

Every release that leaves incomplete work must update `docs/architecture/task-ledger.md` and its changelog with Closed, Partial or Deferred status. A later release closes a task only when implementation evidence and a maintained test exist. Do not silently drop open architecture tasks between ZIP releases.

## Interaction, stacking, input and frontend-failure release gate

Every release that adds or changes a visual object, toolbar, overlay, editor mode, embedded web runtime or media interaction must begin with an explicit checklist in the current changelog. Do not mark an item complete until the relevant repository contract test passes.

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
## Documentation viewport-decoration containment

Documentation cursor paws, paw trails, click bursts, hover sparkles, satellites, stars, and similar decorative effects must not change document geometry. Pointer-following/transient effects must live inside the dedicated fixed `.publisherstudio-pointer-overlay`, which is viewport-sized, paint/layout contained, clipped, pointer-transparent, and explicitly excluded from the documentation body content-stacking selector. Use `clientX`/`clientY` coordinates only for effects inside that viewport overlay. Never append transient pointer decorations directly to normal body flow.

A documentation-background or decorative-only request must not change article, navigation, footer, rail, scroll, sizing, or stacking behavior unless the task explicitly asks for such a layout change. The regression contract is simple: moving the pointer, creating trails, or animating decorative objects must not change `scrollWidth` or `scrollHeight`. `build/Assert-DocumentationPointerOverlay.ps1` enforces the source-level containment contract on normal builds.

Generated documentation roots are replace-not-merge artifacts. Before publishing a new `PublisherStudio-<version>.pdf`, clear the generated help-docs destination with retry-aware cleanup and remove any stale `PublisherStudio-*.pdf` whose name does not match the current source version. GitHub Pages snapshot preparation may prune stale generated versioned PDFs only when the expected current PDF is present; it must never silently accept a missing current PDF. This contract exists to prevent a successful documentation build from failing later because an older generated handbook survived a locked or partial directory cleanup.

