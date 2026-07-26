# Interaction, stacking and notification architecture (v1.0.88)

## One object, one authoritative layout policy

Mainframe objects, Panel Studio elements, automation callers and future media-suite object layers must commit geometry and layer changes through `IPublicationElementLayoutService`. UI code may provide optimistic browser feedback, but persisted X/Y/width/height and Z-order are normalized by the service. The same policy is exposed through `PublicationLayoutController` for LocalGPT, AICouncil and other open clients.

Input adapters do not own document semantics. Mouse, pen, touch, keyboard, gamepad and automation resolve to the same commands: select, constrain/commit bounds, nudge, duplicate, delete and move layer. This prevents separate controller-specific or touch-specific object models.

## Embedded interactive content

An iframe, canvas, map or live widget has two explicit designer states:

1. **Arrange object** — a local transparent shield owns pointer input so the outer publication element can be selected, moved and resized.
2. **Interact with content** — the shield is absent and the embedded runtime receives input.

Designer shields are UI-only and are not persisted or exported. HTML, structured website, SVG/raster snapshot and video-export paths continue to operate on the authored object/runtime.

## Asynchronous interaction rule

A component field that can be cleared by `dragend`, disposal, navigation or another event must never be dereferenced after an `await` unless it was captured before the await and the owning transaction is validated afterward. `PanelStudio.DropDraggedElement` captures draft, view, prototype and existing ID; clones the prototype only after validation; and clears transient drag state in `finally`.

Browser disconnect and cancellation are expected lifecycle outcomes. They are logged at debug level and do not notify the user. Recoverable application failures are logged at warning/error level and published through `IUserNotificationService`.

## Stacking contexts

Z-index is meaningful only inside an explicit owner:

- publication object Z-index belongs to the publication page or panel view;
- object hit layers and drag previews belong to their studio canvas;
- component designer shields belong to the object content wrapper;
- modal/backdrop/toast levels belong to the application shell.

Local interaction features must not use arbitrary global values such as 9000, 10000 or `int.MaxValue` merely to become visible. The owning host uses `isolation:isolate`, and children use a small documented local scale. Persistent object Z-index is not offset by designer-only constants.

## Logging and user feedback

Every new or materially changed service receives `ILogger<T>`. Frontend-facing errors also go through `IUserNotificationService`; UI components must not rely on an unstructured `_error` string alone. The notifier is scoped to the Blazor circuit, while log output remains suitable for host diagnostics. API and background-only services log without attempting to access a circuit notifier.

## Mandatory recurring release gate

Every changelog must add and complete the interaction/stacking gate when a new interactive component, toolbar, overlay, export renderer or embedded runtime is introduced. Tests must cover Mainframe, owning studio, pointer/touch/keyboard/controller routes, context/properties, Z-order, exports, logging and notifications. A missing device/runtime test stays **Partial** in the task ledger rather than being implied as complete.
