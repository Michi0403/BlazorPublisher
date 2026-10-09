# PublisherStudio 4.1.8 — Panel Studio component switch type repair

PublisherStudio 4.1.8 is a focused compiler follow-up to 4.1.7.

## Fixed

- `PanelDocumentService.CreateComponentTool(...)` now target-types its heterogeneous switch expression as `PublicationElement`.
- Every switch arm already produces a `PublicationElement` subtype (`TextFrameElement`, `ImageFrameElement`, media/shape/data/component/live/HTML/panel elements); the explicit target removes the `CS8506` best-type inference failure without changing component creation behavior.
- The selected result still receives `Visible = true`, preserving the 4.1.6 on-the-fly Panel Studio behavior.
- The 4.1.7 XML `<param>` documentation repair is retained.

## Validation boundary

Source-only validation was performed. `dotnet`, MSBuild, NuGet restore, publish and GitHub were not invoked.
