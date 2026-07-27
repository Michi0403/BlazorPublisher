# PublisherStudio / BlazorPublisher 2.0.1 final source review

This source candidate closes the reported release icon, mixed-language, automatic automation polling and Panel/Div Studio persistence regressions while preserving the Organic 1-Wire 2.1 work.

## Release packaging

`Build-Release.ps1` explicitly copies the repository-owned `assets/PublisherStudio.ico` into application and setup outputs before validating the setup payload. This avoids relying on RID/single-file Content-item propagation.

## Panel / Div Studio

- Arrange mode renders the real shared publication elements beneath the interaction hit layer instead of placeholder-only boxes.
- HTML iframe content is eager and visible in Arrange mode while pointer input remains owned by the move/resize layer.
- Double-click switches to real interaction mode without changing the saved graph.
- Opening a standalone HTML/DIV object creates a local panel draft with a new descendant ID and local coordinates.
- Adding another object promotes the original HTML/DIV object to a true `PanelElement`; the complete authored graph replaces the original Mainframe object while preserving Mainframe position, size, rotation, layer and interaction metadata.
- Save commits an isolated normalized clone and refuses to overwrite a stale or changed Mainframe target.
- Nested element IDs are normalized globally through the panel graph, preventing root/child duplicate IDs.
- Stable Blazor `@key` identities and z-order normalization protect reopening and rerendering.

## Project formats

- `.publisher-panel.json` is the lossless native Panel/Div Studio project format.
- `.canvas` implements open JSON Canvas nodes and layer order, with an optional `publisherStudioElement` extension for lossless PublisherStudio round trips.
- HTML remains a sandboxed web-content import/delivery format and is not falsely presented as a complete editable panel project.

## Localization and browser automation

English and German catalogs have identical key sets and include all statically identifiable UI labels/tooltips/placeholders. Partial word translation is rejected, preventing mixed-language controls. Browser automation is disabled by default, requires explicit per-tab activation and remains gated by an approved linked 1-Wire peer.

## Validation boundary

All 47 included Node source/architecture/runtime-contract groups pass. This delivery environment cannot execute the Windows .NET 10, PowerShell or licensed DevExpress runtime, so the maintainer's clean Windows build and browser interaction test remain the definitive runtime proof.
