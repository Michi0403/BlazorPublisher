# PublisherStudio 3.9.4 — presentation navigation and viewport workbench recovery

## Fixed

- Restored the standalone **interactive presentation HTML** runtime contract. The broken export could contain DevExpress maintenance `DxFormLayout` nodes between `.website-publication` and `.print-page`, while the presentation runtime still searched only direct page children. That yielded zero presentation pages and prevented the navigation/replay control bar from being created.
- Presentation/site/video/raster export discovery now finds real `.print-page[data-page-id]` descendants defensively. Before serializing standalone HTML, the cloned publication is normalized so its pages are direct publication children and maintenance-only editor wrappers are not emitted as part of the standalone publication structure.
- Interactive presentation playback controls default to visible unless explicitly disabled and provide previous-page, replay-current-page, next-page and fullscreen actions. This is an HTML-export behavior; it does not depend on a playback-control flag being present in the publication JSON.
- Expanded Spreadsheet, Media Studio, Picture, Data Visual, Data Manager, Component, Streaming, Panel, Story, Barcode and Media Converter workbenches to approximately 92% viewport width by 90% viewport height, while keeping shared DxPopup viewport containment and internal scrolling. Page Effects stays bounded to its content-oriented responsive footprint instead of becoming a mostly empty full-screen shell.
- Kept small configuration/export-choice dialogs content-oriented rather than forcing every confirmation window to become a full workbench.
- Retained the recovered Publisher mainframe/rulers, Data Visual contrast and checkbox alignment, shape corner radius, Page Effects containment, and existing InteractiveServer boundaries.

## Validation

- PublisherStudio service sources are byte-identical to 3.9.3.
- Application architecture, Razor maintenance, async-continuation, service-resilience, cross-platform, prerender interop, component-resilience and Panel Studio persistence audits pass.
- Node syntax validation passes for all 19 maintained PublisherStudio browser JavaScript files.
- JavaScript diagnostics hash manifest matches all 16 tracked files after the export-runtime change.
- Explicit `@rendermode` locations are unchanged from 3.9.3.
- No `dotnet`, MSBuild, NuGet restore/publish, GitHub or online repository access was used for this source-only handoff.
