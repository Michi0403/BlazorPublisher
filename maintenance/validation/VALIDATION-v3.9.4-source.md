# PublisherStudio 3.9.4 source validation

## Validation boundary

This handoff was validated at source level only. No `dotnet`, MSBuild, NuGet restore/publish, installer execution, GitHub, or online repository access was used.

## Evidence-driven frontend recovery

- Large workbench `DxPopup` surfaces use a definite viewport-relative size (`92vw` × `90dvh`) and retain the existing shared popup overflow/containment contract. Page Effects uses a bounded responsive footprint (`min(1180px, 92vw)` × `min(820px, 90dvh)`) to avoid the previously reported empty-shell geometry.
- Interactive presentation HTML export no longer depends on `.print-page` being a direct child of the live editor's publication surface. Export discovery finds `.print-page[data-page-id]` descendants, then normalizes the cloned standalone publication to direct page children before serialization.
- The presentation runtime defaults playback controls to visible unless explicitly disabled and creates previous, replay, next and fullscreen controls.
- PublisherStudio service source is unchanged from 3.9.3.
- Explicit `@rendermode` locations are unchanged from 3.9.3.

## Source checks run

- Application architecture audit: passed.
- Razor maintenance architecture audit: passed for 48 Razor components.
- Async continuation audit: passed for 82 source files, 1108 await tokens, 433 `ConfigureAwait(false)`, 621 renderer-affine `ConfigureAwait(true)`, 49 configured async disposals and 5 configured async streams.
- Service resilience audit: passed for 1389 service methods and 3 iterator/yield methods.
- Cross-platform boundary audit: passed with 60 checks.
- Prerender JavaScript interop safety audit: passed for 3039 component methods.
- Component resilience audit: passed for 3039 component methods.
- Panel Studio persistence source audit: passed after refreshing the tracked `publisherInterop.js` hash.
- Node syntax validation: passed for 19 browser JavaScript files.
- JavaScript diagnostics manifest: all 16 tracked hashes match.

## Result

The source-level contracts for the 3.9.4 popup/workbench and interactive-presentation export recovery are satisfied. A real browser/runtime export test remains required to prove the resulting control bar and DevExpress popup geometry on the target installation.
