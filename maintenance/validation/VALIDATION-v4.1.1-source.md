# PublisherStudio 4.1.1 source validation

## Scope

Source-only validation for the Panel Studio / DevExpress lifecycle diagnostics enhancement. No .NET build, restore, publish, PowerShell build gate, GitHub or online repository access was used.

## Runtime evidence used

- The supplied runtime log shows the blank Panel Studio preset being created and the DevExpress Blazor `Cannot read properties of null (reading 'addEventListener')` failure occurring before the Panel Studio interaction binding reports `PreviewRevision:0 AuthoringRevision:0`.
- The failure is therefore earlier than authored-element mutation and needs browser-side lifecycle evidence at the moment DevExpress fails.
- The supplied PublisherStudio 3.7.6 source remains the known-good structural reference; 4.1.1 keeps the 4.1.0 topology change based on that comparison.

## Added diagnostics

- `javascript-diagnostics.js` keeps a bounded, observational lifecycle trace using the native `MutationObserver` API without replacing browser/vendor lifecycle primitives.
- Matching DevExpress/Panel Studio JavaScript failures include a structured lifecycle snapshot in the forwarded stack text.
- `publisherInterop.js` exposes `capturePanelStudioLifecycle` for explicit C#-initiated browser snapshots.
- `PanelStudio.razor` exposes stable diagnostic correlation attributes and logs initialization, pre-bind, binding-active and binding-failure lifecycle data.

## Source checks performed

- `node --check` passes for `wwwroot/js/javascript-diagnostics.js`.
- `node --check` passes for `wwwroot/js/publisherInterop.js`.
- JavaScript diagnostics manifest hashes were refreshed only for the two intentionally changed maintained JavaScript files.
- The diagnostics runtime still contains the required global error/unhandled-rejection forwarding and does not replace EventTarget, timer, animation-frame, microtask or observer constructors.
- No DevExpress vendor file was modified.
- The complete `build/*.ps1` script set is byte-for-byte unchanged from 4.1.0.
- Release identities for PublisherStudio.Web, InstallerConsole, npm package/lock and application browser cache-busters are aligned at **4.1.1**.

## Runtime verification still required

A Windows/DevExpress runtime run is required to identify the exact remaining failing custom element. If the same exception recurs, the application log should now contain a `PublisherStudio Panel/DevExpress lifecycle snapshot` on the JavaScript error plus correlated `Panel Studio initial browser lifecycle snapshot` / binding records. That evidence is intended to make the next code repair deterministic.
