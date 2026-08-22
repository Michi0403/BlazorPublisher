# PublisherStudio 2.9.6 — Signal, media interaction, and local template libraries

## Interaction release gate

- [x] Canonical object-layer participation: publication templates load through `PublicationFileService`; Div templates remain ordinary `PanelElement` trees and insert through `EditorStateService`, so Mainframe selection, transform, duplicate/delete, Z-order, export, print and Panel Studio continue to use the existing canonical object path.
- [x] Nested Panel/Div signal participation: signal target discovery recursively includes Panel views and their children, while rendered Panel children retain canonical `data-element-id` identities.
- [x] Input ownership: native audio/video controls and native form controls own their click sequence; publication wrapper behaviors and signal `OnClick`/`OnDoubleClick` handlers do not process the same native-control click.
- [x] Component interaction routing: maintained DevExtreme component events are bridged into the signal runtime instead of stealing native DOM-control clicks.
- [x] Media signal routing: `OnPlay`, `OnPause`, and `OnEnded` are available; signal-originated media operations are briefly suppressed from feeding back into their own trigger chain.
- [x] Method automation: signal `Call Method` uses the maintained publication-object allow-list rather than exposing arbitrary internal DevExtreme methods.
- [x] Stacking: the New-from-template chooser uses the existing modal stacking variable and the Panel Library extension stays in its existing local dock stacking context; no application-wide maximum Z-index was introduced.
- [x] Responsive interaction: the template chooser collapses to one column on narrow viewports and leaves existing editor input ownership unchanged.
- [x] Cleanup and diagnostics: existing signal-runtime binding disposal remains authoritative; new Service and component failure boundaries use structured logging and recoverable UI failures remain isolated.
- [x] Regression coverage: `build/audit_release_2_9_6.py` checks native-control ownership, media recursion suppression, component event bridging, signal target/method coverage, template discovery/seeding, starter templates, localization parity and version wiring.

## Fixed

- Fixed exported/previewed native `<audio controls>` and `<video controls>` clicks reaching the surrounding publication-element click action after the browser had already handled the media control. This was the cause of immediate play/pause toggling and repeated retrigger loops in authored publications.
- Page/panel publication interaction handling now leaves native media and form-control click ownership to the native control.
- Signal `OnClick` and `OnDoubleClick` no longer consume clicks originating inside native media/form controls.
- Signal-originated `play`, `pause`, `togglePlayback`, `mute`, `unmute`, `setVolume`, and `seek` operations mark the media event briefly so `OnPlay`/`OnPause` automation cannot recursively invoke itself.

## Signal connector coverage

- Added signal triggers for `OnPlay`, `OnPause`, `OnEnded`, `OnItemClick`, `OnSelectionChanged`, `OnValueChanged`, `OnSubmit`, row insert/update/remove, scheduler appointment add/update/delete, and entered chat messages.
- Added signal `Call Method` completion with a target-specific maintained method list.
- Common object methods include click/focus/blur/change/show/hide/toggle visibility, enable/disable/set value, component refresh/repaint/reset operations, grid selection/filter operations, and maintained media operations.
- Signal target selection now recursively exposes objects inside Panel/Div Studio content rather than only top-level page objects.

## Local template libraries

- Added `%LOCALAPPDATA%\PublisherStudio\PublisherTemplates` for complete publication JSON templates.
- Added `%LOCALAPPDATA%\PublisherStudio\DivTemplates` for reusable Panel/Div JSON templates.
- Starter template files are copied from shipped configuration only when the local destination file does not already exist; PublisherStudio never overwrites an edited local template during seed refresh.
- Template discovery is top-level-only and accepts file-only JSON identifiers, rejecting traversal/alternate paths.
- **File → New from template** opens a responsive chooser for local complete-publication templates without changing the existing blank New command.
- The Panel Library shows local Div templates in addition to built-in presets.
- Panel Studio exposes local Div templates as ordinary palette tools.
- Repeated insertion regenerates element, group, connector-port, animation, behavior, component/action/field, media-segment, panel-view and shared-component identities and remaps template-local references before the ordinary normalization/insertion path runs.

## Shipped starter content

- `Photo Blog`: four authored pages with transitions and object animations.
- `Business Presentation`: five authored pages with transitions and object animations.
- Div templates: Media Hero, Two-view Info, and KPI Strip.

## Localization and release wiring

- Added synchronized template/signal UI strings to all six maintained localization catalogs; all catalogs contain 3,327 keys in exact parity.
- Updated PublisherStudio Web, InstallerConsole, npm package metadata and browser resource cache markers to **2.9.6**.
- Preserved DevExpress **25.2.9** and existing deployment architecture.

## Validation status

Closed for the requested source changes. Source-only repository audits pass for architecture, async continuations, component resilience, prerender interop safety, service resilience, iterator policy, Panel Studio persistence, XML documentation, JavaScript syntax, JavaScript diagnostics hashes, and the 98-check 2.9.6 release audit. No `dotnet` build/run was performed because this environment does not provide the .NET toolchain, per the requested workflow.
