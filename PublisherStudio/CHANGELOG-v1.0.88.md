# PublisherStudio v1.0.88

## Interaction and stacking release gate

This checklist is now a mandatory release gate for every new interactive surface. It is entered into the changelog before implementation and may be marked complete only when the related repository contract remains green.

- [x] **Mainframe:** normal publication-object selection, move, resize, rotation, deletion and layer ordering are retained for HTML/Web/3D content.
- [x] **Panel / Div Studio:** palette insertion and existing-object arrangement use separate, circuit-safe paths.
- [x] **Mouse and pen:** pointer capture moves and resizes the selected object without handing ownership to embedded content.
- [x] **Touch and tablet:** Pointer Events, `touch-action` ownership and coarse-pointer resize targets are present.
- [x] **Keyboard:** arrow nudging, Delete, duplicate and complete layer-order shortcuts are wired.
- [x] **Controller / Steam Deck:** standard Gamepad API mappings provide nudge, duplicate, interaction mode and layer ordering.
- [x] **Z-order:** designer shields, hit layers, drag ghosts and media-drop overlays use local stacking contexts instead of global z-index escalation.
- [x] **Context and properties:** selected objects remain selected, expose the inspector/context menu and all destructive commands target the selected ID.
- [x] **HTML export:** arrangement-only shields are omitted outside designer mode; authored HTML/Canvas/3D runtime remains interactive.
- [x] **Render export:** the designer-only shields are not part of the exported publication surface and existing raster/video snapshot behavior is retained.
- [x] **Logging:** new/touched services and browser-to-Blazor interaction failures write structured logs.
- [x] **User notification:** frontend-facing failures are forwarded through the shared circuit-scoped notification service.
- [x] **API reuse:** geometry constraints and layer reordering are available through `IPublicationElementLayoutService` and `/api/publication/layout`.

## Fixed

- Fixed the `PanelStudio.DropDraggedElement` race that caused a `NullReferenceException` at `element.Id = Guid.NewGuid()` when HTML `dragend` cleared `_dragPrototype` while the awaited coordinate lookup was still running.
- The drop operation now snapshots draft, view, prototype and existing ID before awaiting JavaScript, verifies that the editor transaction still exists afterward, clones the captured prototype, and always clears drag state in `finally`.
- Normal browser disconnect/cancellation paths no longer escalate into an unhandled Blazor circuit failure. The subsequent `JSDisconnectedException`, `ObjectDisposedException` and failed reconnect messages were consequences of the original circuit crash and are now handled at their owner boundary.
- HTML/Web/Canvas/3D embeds now have a designer-only pointer shield. In Mainframe and Panel Studio arrange mode they behave like ordinary publication objects; in interaction preview and exports the shield is absent.
- Panel Studio existing objects no longer depend on native HTML drag events. Pointer capture now owns move and resize for mouse, pen and touch, preventing `dragend` timing from mutating managed state.
- Replaced local `9000`, `10000`, `10001`, `10010` and near-maximum map-shield z-index values with explicit local stacking contexts and bounded local levels.
- Introduced one application stacking scale for modal, dialog, notification and export surfaces. All application-owned numeric CSS/JavaScript z-index values are now bounded to `5000` or lower; vendor CSS remains vendor-owned.

## Added and enhanced

- Added `IPublicationElementLayoutService` as the shared geometry/z-order policy for components, controllers, automation and future media-suite object layers.
- Added `PublicationLayoutController` endpoints for constraint and layer reorder operations.
- Added four complete layer commands: bring to front, bring forward, send backward and send to back in Panel Studio ribbon/context menu, Mainframe keyboard routing and controller routing.
- Added a scoped `IUserNotificationService` plus `UserNotificationHost` to surface recoverable frontend failures without tearing down the application circuit.
- Added structured logging to the new layout/notification paths and to browser input and screenshot queues.
- Added browser callback error forwarding from Mainframe to `ILogger<PageSurface>` and the shared notifier.
- Added coarse-pointer resize handles and focus-visible states for tablet and controller navigation.
- Added a regression suite that protects the drag transaction, HTML/3D object ownership, input modalities, local stacking, layout API, logging, notifier integration and this recurring release gate.
- Added the same interaction/stacking/input/frontend-failure gate to `AGENTS.md`, so future visual work must begin with the checklist instead of rediscovering the failure class after integration.

## Compatibility

- Publication format remains `1.55`.
- Existing panel documents, reusable modules, HTML exports, structured websites, media exports and OpenSCAD/3D runtime payloads are unchanged at the persisted-data boundary.
- The existing HTML drag path remains available for inserting palette prototypes. Existing-object movement is intentionally pointer based.

## Task status

- **Closed:** reported Panel Studio `NullReferenceException` and its Blazor circuit-disconnect cascade.
- **Closed:** Mainframe arrangement ownership for HTML/Web/Canvas/3D embeds.
- **Closed:** Panel Studio move/resize, selected-ID deletion, complete layer ordering, context menu, keyboard, pointer, touch and standard gamepad mappings.
- **Closed:** local stacking correction for the newly introduced/identified designer shields, Panel Studio hit/drop layers and studio file-drop overlays.
- **Closed:** logging and user-notification contracts for all new/touched v1.0.88 interaction and automation services.
- **Partial:** repository-wide migration of every historical service to an interface and structured logger remains an incremental touched-code rule; the legacy inventory is still authoritative.
- **Partial:** hands-on acceptance across every physical touch device/controller and licensed DevExpress surface requires the native developer-machine build and device tests.
- **Deferred:** operating-system-global input injection is intentionally outside the browser automation boundary.

Open or partial work remains listed in `docs/architecture/task-ledger.md`; no task is silently discarded when a release closes.
