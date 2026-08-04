# Architecture and feature task ledger

Status values are **Closed**, **Partial**, or **Deferred**. A task may be removed only after a later changelog records it as solved and the related contract test remains green.

| Task | Status in 1.0.89 | Evidence / next step |
|---|---|---|
| v1.0.89 LocalGPT normal application/installer port compatibility | Closed | Organic discovery/service ports are separate and optional; LocalGPT restores its public read-only runtime port surface and validates distinct port contracts. |
| v1.0.89 organic 1-Wire DTO, discovery, TCP connection and capability negotiation | Closed at TCP/UDP baseline | Versioned envelopes, bounded message size, SHA-256, CRC32, encrypted/public payload exclusivity and online capability exchange are implemented. UART/SPI/MQTT adapters remain deferred. |
| v1.0.89 per-organ permissions and human approval | Closed for PublisherStudio browser scope | Persistent rules default to AskEveryTime; pending work is approved/declined in the UI or API and results are returned to LocalGPT. |
| v1.0.89 OpenSCAD Team | Closed at canonical source-generation baseline | Council preset and organic capability reuse `IOpenScadDocumentService`; native OpenSCAD invocation remains deferred below. |
| v1.0.89 Spreadsheet Team sequential hand-eye workflow | Closed at inspection/browser-queue baseline | Spreadsheet sessions are inspected read-only and screenshot/input packages serialize per work order; workbook mutation still requires bounded approved input and browser permission. |
| Organic protocol signing, encryption and key management | Partial | Signature/encrypted payload fields and exclusivity are reserved; real signing/encryption waits for explicit trust and key lifecycle design. |
| v1.0.88 Panel Studio drop race and Blazor circuit crash cascade | Closed | Transaction state is captured before JS await, validated afterward and cleared in `finally`; guarded by `interactionAndStackingReleaseGate.test.mjs`. |
| Mainframe/Panel arrangement of HTML, Canvas and 3D web content | Closed | Designer-only shields preserve normal object selection/move/resize while interaction mode and exports remain unshielded. |
| Cross-surface mouse/pen/touch/keyboard/gamepad commands | Closed at browser/device API baseline | Pointer Events, keyboard routes and standard Gamepad API mappings converge on the same object commands. Device-specific acceptance remains part of native release testing. |
| Recurring interaction and stacking changelog gate | Closed for v1.0.88 and mandatory later | Changelog checklist plus architecture/test contract requires Mainframe, studio, input, Z-order, exports, logging and notification review. |
| Local overlay Z-index normalization | Closed for application-owned v1.0.88 CSS/JS | Panel hit/drop layers, web/map shields, studio file-drop overlays and global application surfaces use the documented bounded scale; the release test rejects application numeric z-index values above 5000. Vendor CSS remains vendor-owned. |
| Frontend service logging and notifier integration | Closed for new/touched v1.0.88 services; Partial repository-wide | Layout, notification, automation/screenshot and interaction failures log structurally; frontend failures publish notifications. Legacy services migrate when touched. |
| v1.0.87 browser runtime interpolated raw-string failure (`CS9007`) | Closed | Runtime JavaScript uses a non-interpolated raw template and explicit payload marker; guarded by `browserRuntimeTemplateSafety.test.mjs`. |
| v1.0.86 .NET SDK duplicate localization Content items (`NETSDK1022`) | Closed | Project-local localization files use `Content Update`; guarded by `sdkDefaultItems.test.mjs`. |
| v1.0.84 `Math.Hypot`, CA1859 and IDE0305 compiler findings | Closed | Replaced and protected by compilation-safety/interchange tests. |
| Temporal selection layers, point ownership, selected deletion and HTML compatibility marking | Closed | Delivered in 1.0.83 and retained by video selection/layer tests. |
| Reusable interface-first service composition and explicit lifetimes | Partial | New and touched areas use interfaces/lifetime descriptors. The raw static baseline and priority candidates are maintained in `static-service-migration-inventory-v1.0.85.md`; remaining legacy candidates migrate when touched. |
| Controller/API access for newly reusable services | Closed | OpenSCAD, video interchange/geometry/runtime, code, automation, screenshots, localization/paths and render capability APIs exist. |
| LocalGPT/AICouncil mouse and keyboard commands | Closed for browser scope | DOM/pointer/keyboard queue is implemented. operating-system-global input remains deferred. |
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

| v2.0.0 synchronized LocalGPT protocol package authority | Closed at package/source baseline | PublisherStudio removed its protocol source project, restores package 2.0.0, and release builds download the authoritative LocalGPT asset. Native release-build evidence remains maintainer-run. |
| v2.0.0 paired Story Editor Council proposal | Closed at reviewable text baseline | Linked-only ribbon request, Council correlation, retained proposal and explicit user insertion are implemented and source-tested. |
| v2.0.0 bidirectional frontend authority | Closed at browser/frontend baseline | Transport and link authorization are separate; per-capability rules and receiving-frontend confirmation/input remain authoritative. Cryptographic peer identity remains Partial. |
| v2.0.0 screenshot double-confirmation | Closed for same-origin browser capture | PublisherStudio approval plus browser current-session permission are required. Cross-origin/DRM capture remains Deferred. |
| v2.0.0 native licensed build verification | Deferred in this environment | Michael must run exact .NET 10/DevExpress Debug and Release builds and runtime acceptance on supported platforms. |
| v2.0.1 compiler/build correction | Closed at source-contract baseline | Restored `System.Text.Json` Razor visibility, normalized nullable import inputs, advanced app metadata and retained protocol package 2.0.0. Native build evidence remains maintainer-run. |
| v2.0.2 installer, launcher, optional 1-Wire and recording-preview repair | Closed at source-contract baseline | Restores legacy wrapper asset paths, stages a launcher-promoted setup repair executable, merges runtime files with manifest ownership, hash-preserving stale cleanup and rollback, retains the installed runtime architecture, removes broad legacy MediaHost deletion, disables automatic 1-Wire connection by default, and reattaches long-running recording previews. Native Windows/DevExpress acceptance remains maintainer-run. |
| v2.0.3 Windows build-policy compatibility repair | Closed at source-contract baseline | Separates architecture-audit output from exit status, fixes iterator parsing and uncovered iterators, makes publish profiles explicit, and validates setup launch profiles with PowerShell 5.1-safe exact-name checks. Native Windows/DevExpress build evidence remains maintainer-run. |
