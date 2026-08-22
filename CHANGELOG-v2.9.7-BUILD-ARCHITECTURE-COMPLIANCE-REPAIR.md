# PublisherStudio 2.9.7 — Build and architecture compliance repair

## Interaction and frontend-failure release gate

- [x] Retains the 2.9.6 canonical publication/Panel object paths, signal routing, native media-control ownership, template-library integration and existing InteractiveServer boundaries.
- [x] The new publication-template chooser now owns the repository-required catch/log/user-notification failure boundary.
- [x] Panel Library local-template discovery now also surfaces recoverable failures through `IUserNotificationService` instead of relying only on inline error state.
- [x] Panel Studio local Div-template discovery keeps its existing tools available and surfaces a recoverable warning without tearing down the circuit.
- [x] The Panel Studio palette tool factory no longer declares colliding `templateId` locals across sibling branches.
- [x] Shipped template JSON remains covered by the existing `Configuration\**\*` output/publish contract.
- [x] No render-mode boundary was moved or removed; routed interactive pages remain authoritative and nested editor components inherit their circuit.
- [x] No new architecture root, transport boundary, static application state, package, process or database/schema migration was introduced.

## Fixed from authoritative Windows build feedback

- Fixed `Components/Editor/NewPublicationDialog.razor` failing `Assert-ComponentDiagnostics.ps1` because it was a new operational component without a user-notification call. The component now injects `IUserNotificationService` and reports template-discovery failures with `Notifications.Error(...)` in addition to structured logging and inline error state.
- Fixed `Components/Editor/PanelStudio.razor` compiler error **CS0136** by separating the local identifiers used for LocalApplicationData Div templates (`divTemplateId`) and document component templates (`componentTemplateId`).
- Extended the same recoverable frontend-failure policy to the changed Panel Library and Panel Studio template-discovery boundaries.

## Retained 2.9.6 functionality

- `%LOCALAPPDATA%\PublisherStudio\PublisherTemplates` complete-publication templates and `%LOCALAPPDATA%\PublisherStudio\DivTemplates` reusable Panel/Div templates remain non-destructively seeded and user-editable.
- File → New from template, Panel Library local Div templates, Panel Studio Div-template tools, fresh identity regeneration/remapping, and the Photo Blog / Business Presentation starter publications remain intact.
- Native audio/video controls keep ownership of their own click sequence, preventing the authored wrapper and signal connector from immediately toggling playback a second time.
- Signal connectors retain media/component triggers, nested Panel/Div target discovery and the maintained public Call Method allow-list.
- DevExpress/DevExtreme remains **25.2.9** and the application continues to target **net10.0**.

## Version policy

- PublisherStudio Web, InstallerConsole, npm package metadata and browser cache markers are aligned at **2.9.7**.
- The single-digit minor/patch policy remains satisfied; no `2.9.10`-style version was introduced.

## Validation status

This source release was not compiled in the preparation environment because the .NET/DevExpress toolchain is intentionally unavailable here. The authoritative Windows build feedback supplied for 2.9.6 was used directly for the two reported failures. Source audits for architecture, async continuations, component method resilience, prerender interop safety, service resilience, iterator policy, Panel Studio persistence, JavaScript syntax, template/release wiring and the repository-equivalent new-component diagnostics rule pass for 2.9.7.
