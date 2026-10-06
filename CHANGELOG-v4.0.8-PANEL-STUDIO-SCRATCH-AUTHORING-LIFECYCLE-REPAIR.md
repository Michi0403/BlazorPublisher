# PublisherStudio 4.0.8 — Panel Studio scratch-authoring lifecycle repair

## Summary

PublisherStudio 4.0.8 repairs the runtime regression where a Panel Studio document created from a blank panel could accept components into its mutable view model and show them in the component inspector while the authored canvas remained on its pre-insert render. The known-good PublisherStudio 3.7.6 Panel Studio was compared directly with 4.0.7 before making this repair.

## Fixed

- Panel Studio now owns an explicit authored-render revision. Every maintained mutation path that changes panel/view/element output advances that revision and supplies it to `PanelView`, so a mutable draft is never dependent on reference-equality heuristics to notify the child renderer.
- Structural insert/remove/duplicate operations keep the existing `PanelView` and keyed live element instances instead of remounting the whole panel. This preserves cameras, media, DevExtreme controls and other live content while still making newly inserted scratch-panel components visible immediately.
- Pointer/keyboard geometry commits, layer moves, nudges, imported media, complete panel imports, advanced JSON replacement, canvas/layout changes and maintained type/data-mode changes all invalidate the authored render explicitly.
- The DxPopup interaction-surface attachment fallback no longer calls `StateHasChanged` repeatedly while the DevExpress popup portal is still materializing. It now performs a bounded non-rendering attachment poll, avoiding Blazor/DevExpress component-content churn during the exact lifecycle window implicated by the reported `componentContentChanged` / null `addEventListener` browser failures.
- Panel Studio text-only DevExpress list buttons no longer inherit the generic Visual editor icon-column grid or `overflow-wrap:anywhere`. View/component names retain normal whole-word wrapping instead of collapsing into one-character columns in narrow sidebars.

## Preserved

- The 4.0.7 compiler repairs, 4.0.6 text-service ownership repair and 4.0.4 direct structural `PanelView` geometry contract remain in place.
- Existing Panel Library documents continue through the same panel model, renderer, data connections, behaviors and export paths.
- No publication schema or persisted document format changes.
- No build or maintenance script changes.
- No native-control rollback; DevExpress ownership remains intact.

## Validation scope

This source package was validated without running .NET/MSBuild. The maintained Python source audits available in the repository were run where applicable, the `build/` tree was compared against 4.0.7, source references/version identities were checked, and ZIP integrity was verified. Runtime confirmation of the reported browser behavior still belongs to the user's normal Windows/DevExpress build/run environment.

## Version

`4.0.7 -> 4.0.8`.
