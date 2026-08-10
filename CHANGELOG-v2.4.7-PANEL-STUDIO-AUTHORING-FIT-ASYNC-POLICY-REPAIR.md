# PublisherStudio 2.4.7 — Panel Studio authoring fit and async policy repair

## Fixed

- Panel Studio now wraps the selected panel in a dedicated authoring design frame instead of letting generic `publication-panel` sizing compete with the editor workspace.
- The selected panel is centered and uniformly zoomed to fit the available Panel Studio workspace. Its internal `CanvasWidth` / `CanvasHeight`, aspect ratio, hitboxes, drop coordinates and persisted element geometry remain unchanged.
- Removed the 2.4.5/2.4.6 shrink-only zoom cap that left a 160 × 90 panel physically tiny on large/high-DPI workstations. The neutral gray area remains only the editor stage; it is not the panel or the publication Mainframe.
- Panel Studio keeps the shared authoring viewport introduced in 2.4.4: live renderer, selection hitboxes, drag, resize and drop conversion still resolve against the same panel-local viewport.
- `ConfigureAwait(true)` in `PanelStudio.razor` is now restricted to the `OnAfterRenderAsync` lifecycle binding/layout initialization. All other explicit awaits use `ConfigureAwait(false)`, including import/export, pointer cancellation, context menu, save and disposal paths.
- The three asynchronous stream disposals are explicitly configured through `ConfiguredAsyncDisposable`.
- `DocumentationViewerHost` no longer uses a renderer-affine true continuation outside a lifecycle method.
- The async continuation guard now permits `ConfigureAwait(false)` in Razor components and rejects `ConfigureAwait(true)` outside reviewed component lifecycle/initialization methods. The Panel Studio baseline is tightened to zero unconfigured awaits, two lifecycle `true` awaits and at least twenty-two `false` awaits.
- The Panel Studio geometry guard now requires the dedicated design frame and the fit-to-workspace zoom contract so the tiny-panel regression cannot be reintroduced silently.

## Versions

- `PublisherStudio.Web`: 2.4.7
- `PublisherStudio.InstallerConsole`: 2.4.7

No .NET build, restore, publish or GitHub/network repository access was performed while preparing this source package.
