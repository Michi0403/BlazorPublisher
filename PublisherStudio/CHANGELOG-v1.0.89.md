# PublisherStudio v1.0.89

## Interaction and stacking release gate

The organic-plugin console is a routed management page and does not add a new object layer to the publication canvas. The recurring gate was reviewed before implementation.

- [x] **Mainframe:** no publication-object selection, move, resize, rotation, deletion, context, properties or layer-order behavior was changed.
- [x] **Panel / Div Studio:** no palette/drop/arrangement path was changed.
- [x] **Mouse and pen:** the new page uses ordinary bounded controls; no canvas pointer owner was introduced.
- [x] **Touch and tablet:** controls remain standard browser inputs/buttons and do not intercept publication touch gestures.
- [x] **Keyboard:** normal tab/focus behavior is retained; no global shortcut was added.
- [x] **Controller / Steam Deck:** no global controller mapping or canvas ownership was changed.
- [x] **Z-order:** the page uses normal document flow and no new numeric z-index.
- [x] **Context and properties:** the editor state and selected object remain untouched when opening the separate route.
- [x] **HTML export:** the management route and permission UI are not included in exported publications.
- [x] **Render export:** no raster/video/OpenSCAD export surface was modified.
- [x] **Logging:** discovery, connection, capability execution, permission changes and failures use structured `ILogger<T>` events.
- [x] **User notification:** frontend connection, Council and approval failures are surfaced through `IUserNotificationService`.
- [x] **API reuse:** the new transport delegates to existing automation, screenshot, OpenSCAD, spreadsheet, business-context and FFmpeg/media services.

## Fixed

- Restored compatibility with the LocalGPT bootstrap contract by consuming a dedicated organic service/discovery pair without replacing the normal LocalGPT application/installer port.
- Prevented unapproved eye/hand work from being executed: absence of a matching permission remains `AskEveryTime`.
- Made `Deny` a hard policy outcome that cannot be converted into a one-time approval, and require a non-empty exact work-order key for `CurrentWorkOrder`.
- Kept UDP discovery alive after malformed or transient datagrams and poll-expire stale peers even when no new broadcast arrives.
- Prevented overlapping spreadsheet hand-eye packages by serializing work with one semaphore per work-order key.
- Kept OpenSCAD generation on the canonical `OpenScadDocument` / `OpenScadNode` and registered renderer path instead of introducing a competing model.

## Added

- Added UDP discovery of LocalGPT and a persistent TCP organic-plugin connection with versioned envelopes, SHA-256 integrity, CRC32 transmission checking, payload-size limits and mutually exclusive encrypted/public payload fields.
- Added capability negotiation for browser screenshots (eyes), bounded browser input (hands), canonical OpenSCAD generation, spreadsheet-session inspection, text-insertion proposals, project/API context and existing FFmpeg/media capabilities.
- Added bounded screenshot/input result capabilities so a hand/eye spool can return evidence into the next Council heartbeat instead of stopping after queue submission.
- Preserved explicit completed, failed, declined and cancelled status values returned by LocalGPT instead of treating every work-result envelope as completed.
- Added a persistent per-peer/per-capability/per-organ permission matrix supporting ask every time, same capability, current work order, always allow and deny.
- Added user-gated approval/decline handling and sequential/scheduled work metadata.
- Added the `AI Council` ribbon entry and `/organic-plugins` console for discovery, connection, capability review, permission management and Council requests.
- Added role presets for `OpenSCAD Team` and `Spreadsheet Team`; spreadsheet requests carry the inspect → reason → propose → confirm → bounded-input workflow capabilities.
- Added `/api/organic` endpoints for status, peers, capabilities, permissions, work, results, text proposals, connection and Council submission.

## Compatibility and safeguards

- Application and installer version is `1.0.89`; publication format remains `1.55`.
- The existing installer, launch scripts, ordinary PublisherStudio port resolution and LocalGPT web bootstrap are not replaced by the organic transport.
- Discovery failure is optional and logged; PublisherStudio and LocalGPT continue to run without an organic peer.
- Consequential PublisherStudio controller calls require either a stored matching permission or a current explicit user approval. Council submission itself requires an explicit frontend action.
- Native .NET 10/Razor/DevExpress compilation and physical device/browser permission acceptance remain required on the licensed developer workstation.

## Task status

- **Closed:** protocol DTO, discovery, connection, capability negotiation, integrity/error checking and future encrypted-payload reservation.
- **Closed:** PublisherStudio permission/approval UI and API, sequential work-order gate and reuse of existing eye/hand/OpenSCAD/spreadsheet/media services.
- **Closed:** OpenSCAD Team and Spreadsheet Team request presets across PublisherStudio and LocalGPT organic Council briefing.
- **Partial:** cryptographic signing and actual encryption are reserved by the protocol but not enabled without a key-management design.
- **Partial:** browser eye/hand execution remains subject to browser user-gesture and same-origin restrictions.
- **Deferred:** UART, SPI and MQTT adapters implement the same protocol interfaces in later projects; this release ships TCP/UDP only.
- **Deferred:** operating-system-global input and unattended agent loops remain intentionally outside the browser/user-gated architecture.
