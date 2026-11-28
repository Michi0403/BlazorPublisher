# PublisherStudio 2.4.8 — Panel Studio interop and browser-capture lifecycle repair

## Fixed

- Exposes `refreshPanelStudioDesignSurface` through the actual `window.publisherStudio` namespace used by Blazor JS interop. The ES-module export existed in 2.4.7, but the global bridge entry was missing, causing the Panel Studio surface to fail after rerenders/selections.
- The Panel Studio lifecycle guard now verifies both the module export and the global namespace bridge, so this exact runtime wiring regression is rejected during maintained source validation.
- Repeated identical Panel Studio initialization failures no longer flood the notification rail while diagnostics continue to log the underlying exception.
- Media Studio Stop/Cancel/Dispose releases camera, screen/window and microphone tracks immediately after recorder stop is requested. Recording blob finalization/metadata inspection may continue, but browser privacy indicators no longer remain active merely because post-processing is still running.
- `LiveSourceView` is keyed by source identity and source kind in Panel and Print renderers, so changing a source kind disposes the previous capture component and releases its hardware tracks instead of retaining an obsolete camera/microphone session.
- Every rendered `LiveSourceView` now owns a component-instance runtime capture id. The same publication source may therefore exist in Mainframe, Panel Studio and preview at the same time without those render surfaces stealing or detaching each other's browser MediaStream.
- Changed browser modules use a 2.4.8 cache key so installed/published deployments do not keep stale 2.4.7 interop code in an existing browser session.

## Preserved

- Panel Studio authoring geometry, stable interaction binding identity and the 2.4.7 async-continuation policy remain unchanged.
- Recording output remains retained/downloadable exactly as before; only the live capture tracks are released earlier.
- Mainframe, publication model, exporters, streaming output and LocalGPT/1-Wire contracts are not redesigned by this patch.

## Version

- `PublisherStudio.Web`: 2.4.8
- `PublisherStudio.InstallerConsole`: 2.4.8
