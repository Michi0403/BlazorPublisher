# PublisherStudio 2.5.2 — Panel Studio geometry + documentation repair

## Panel Studio apply/save persistence

- Fixed the standalone HTML/interactive-object apply path that discarded Panel Studio local X/Y/width/height changes when the edited object remained the only visible item in its view.
- A single standalone HTML object remains lightweight only while it still fills the complete panel-local canvas at zero rotation.
- As soon as that object is moved, resized, or rotated in Panel Studio, applying it promotes the Mainframe object to a `PanelElement`, preserving the complete authored local graph.
- Promotion keeps the original Mainframe X/Y/width/height/rotation/Z-order, visibility, grouping, animation, interaction, and connector metadata stable. Panel-local geometry therefore stays local instead of changing page placement.

## Fast drag/resize followed by Save/update module

- Added a browser-side `flushPanelStudioInteractions` operation that waits for the existing serialized Panel Studio .NET invocation queue.
- `Save/update module` and `Save configured module` now await queued pointer/keyboard bounds commits before cloning the reusable template prototype.
- The main Panel Studio Save/Apply path also flushes queued layout commits before normalizing/cloning the panel graph.
- This removes the race where a fast click on Save immediately after pointer-up could snapshot the pre-drag bounds even though the browser preview already showed the new geometry.

## Regression protection

- Extended Panel Studio authoring-geometry and interaction-lifecycle source validators with the new persistence/queue-flush invariants.
- Added `audit_panelstudio_persistence.py` plus a PowerShell wrapper and wired it into normal local/release build preflight.
- Refreshed the normalized SHA-256 JavaScript diagnostics inventory for `publisherInterop.js`.
- Kept the shared canvas/runtime architecture unchanged; no working Mainframe/Panel Studio canvas layout path was replaced.

## Interface naming and localization polish

- Normalized the affected editor heading from `Panel / Div Studio` to the product's established `Panel Studio` name while keeping the serialized `PublisherStudio Panel/Div Studio` interchange format identifier unchanged for compatibility.
- Removed the redundant ` · 3D object` suffix from imported/generated interactive 3D layer names, so an `Interactive 3D blob` no longer appears as `Interactive 3D blob · 3D object` in the editor.
- Filled the German catalog entries for the affected `Interactive 3D blob`, `Panel / dashboard`, insert-panel action, and the normalized Panel Studio import notification.

## InteractiveServer render boundaries

- Verified every routed PublisherStudio page except the intentionally static Error page already carries an explicit InteractiveServer render mode.
- Added `Help.razor` to the maintained render-mode validator, closing the audit gap without adding competing nested child render boundaries.
- Preserved the reviewed page/island render-boundary architecture and the existing interactive JavaScript diagnostics bridge.

## XML documentation

- Extended deterministic XML documentation tooling from the public-only surface to all maintained C# types/methods while continuing to cover public/protected API members.
- Filled missing XML summaries across PublisherStudio and InstallerConsole maintained C# source while leaving generated Designer/g.cs sources untouched.
- XML documentation source audit now passes for **4,891 maintained C# type/method and public API declarations**.
- Added Panel Studio persistence behavior to the editor-workspace documentation.

## Version

- PublisherStudio Web: **2.5.2**
- PublisherStudio InstallerConsole: **2.5.2**
- LocalGPT 1-Wire protocol package version remains **2.1.1** because the protocol contract is unchanged.
