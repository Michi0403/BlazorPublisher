# Architecture and feature task ledger

Status values are **Closed**, **Partial**, or **Deferred**. A task may be removed only after a later changelog records it as solved and the related contract test remains green.

| Task | Status in 1.0.85 | Evidence / next step |
|---|---|---|
| v1.0.84 `Math.Hypot`, CA1859 and IDE0305 compiler findings | Closed | Replaced and protected by compilation-safety/interchange tests. |
| Temporal selection layers, point ownership, selected deletion and HTML compatibility marking | Closed | Delivered in 1.0.83 and retained by video selection/layer tests. |
| Reusable interface-first service composition and explicit lifetimes | Partial | New and touched areas use interfaces/lifetime descriptors. The raw static baseline and priority candidates are maintained in `static-service-migration-inventory-v1.0.85.md`; remaining legacy candidates migrate when touched. |
| Controller/API access for newly reusable services | Closed | OpenSCAD, video interchange/geometry/runtime, code, automation, screenshots, localization/paths and render capability APIs exist. |
| LocalGPT/AICouncil mouse and keyboard commands | Closed for browser scope | DOM/pointer/keyboard queue is implemented. OS-global input remains deferred. |
| Screenshot service/controller workflow | Closed for same-origin browser scope | Queue, capture completion, status and file download implemented. Cross-origin frame capture remains browser-limited. |
| Business-object/service/controller context | Closed | `/api/domain-context` exposes domain, method, lifetime and route relationships. |
| OpenSCAD basic figures, transforms, CSG and typed properties | Closed | Catalog and public node graph cover built-in basic primitives and operations. |
| OpenSCAD animation between assembled parts | Partial | Node transform/alpha tracks use `$t`, easing, loop/ping-pong. Generic parameter animation needs node-specific renderers. |
| Visual OpenSCAD builder | Deferred | Node graph/catalog/renderer architecture is ready; no visual graph UI yet. |
| Native OpenSCAD invocation and exact geometry export | Deferred | Requires installed OpenSCAD process integration, cancellation, sandboxing and output validation. |
| Programming-language editor commands and ribbon | Closed at baseline | Profiles, formatting, comments, analysis API and Story Editor Code tab delivered. |
| Full syntax-colored source editor, LSP and formatter plugins | Deferred | Current workspace reports token spans but is not a complete IDE surface. |
| RichEdit text correction | Partial | DevExpress spell service and English dictionary enabled. Additional open dictionaries need licensing review. |
| File-based application localization | Partial | Infrastructure and four starter cultures delivered; historic literals still need migration. |
| DevExpress component translations | Closed for community languages | German, Spanish and Japanese Blazor/RichEdit satellite packages referenced. |
| Configurable default media/project/export paths | Closed at service/project baseline | App settings, project overrides and API delivered; dedicated settings UI remains future work. |
| Render PNG/JPEG/SVG with video/canvas effects | Closed for current-frame rendering | Canvas/video/same-origin iframe snapshots added and tested. Cross-origin/DRM media remains restricted. |
| Extensible exporter plugin discovery | Partial | Capability interface/controller exists; runtime assembly/plugin discovery remains deferred. |
| Native .NET 10 + licensed DevExpress build validation | Deferred in this environment | Must be run by Michael on a licensed machine; repository architecture, JS, JSON and XML tests are provided. |
