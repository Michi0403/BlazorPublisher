# PublisherStudio 4.0.9 — Panel popup lifecycle and export asset repair

## Summary

PublisherStudio 4.0.9 addresses the runtime evidence from the 4.0.8 build instead of treating the DevExpress browser exception as incidental. The repeated `componentContentChanged` → `initializeComponent` → null `addEventListener` failure is repaired at the Panel Studio popup ownership boundary, and the website export hash failure is repaired by restoring the prepared DevExtreme asset bytes that the repository manifest actually declares.

The known-good PublisherStudio 3.7.6 Panel Studio remains the historical comparison point. Its working authoring surface did not place a portaled popup inside a maintenance FormLayout item.

## Fixed

- Panel Studio no longer wraps `DxPopup` in a maintenance-only `DxFormLayout`/`DxFormLayoutItem`. `DxPopup` portals its live modal root to the document overlay; keeping that portal as the only content of a FormLayout item can leave the FormLayout lifecycle with no local editor root while DevExpress processes `componentContentChanged`, matching the reported null `addEventListener` failure.
- The removed maintenance wrapper is not replaced with native/manual modal ownership. `DxPopup` remains the modal owner. The same DevExpress FormLayout/FormLayoutItem count is retained by moving semantic FormLayout ownership onto the actual local view/navigation/layout/name/canvas editor group in the left sidebar.
- The direct popup body still owns the real `.panel-studio-dialog` surface, preserving the 3.9.2 viewport/scroll contract and the current DevExpress-first editor architecture.
- Empty scratch views now perform one deliberate `PanelView` remount when their first element is inserted. This targets the exact empty→populated transition that differs from Panel Library documents. Once populated, subsequent authoring changes keep the existing keyed live component instances.
- The existing authored-render revision from 4.0.8 remains in place for ordinary mutable graph changes; the one-time first-element remount supplements it rather than replacing it.
- Restored `wwwroot/vendor/devextreme-dist/css/dx.light.css` to the exact prepared DevExtreme 25.2.10 bytes declared by `vendor/devextreme-assets.meta.json`. The 4.0.8 source had nine byte substitutions inside embedded SVG generator comments (`24.0.1` → `24.0.2`), leaving file length unchanged but changing the SHA-256 and therefore correctly tripping the export integrity guard.
- The DevExtreme export hash verification remains strict. No cache/hash/security gate was disabled or weakened, and no preparation/build script was changed.

## Preserved

- The 4.0.8 authored-render revision and readable Panel Studio list labels remain.
- The 4.0.7 Razor/compiler fixes, 4.0.6 text-service ownership repair and 4.0.4 direct structural `PanelView` geometry contract remain.
- Existing Panel Library documents, publication persistence, export model, behaviors, data connections, DevExpress controls and InteractiveServer boundaries are unchanged.
- No publication schema or persisted document-format migration.
- No build or maintenance script changes.

## Version

`4.0.8 -> 4.0.9`. The next patch after 4.0.9 must roll to 4.1.0 under the maintained single-digit version-slot rule.
