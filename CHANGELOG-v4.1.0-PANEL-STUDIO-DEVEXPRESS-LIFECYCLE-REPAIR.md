# PublisherStudio 4.1.0 — Panel Studio DevExpress lifecycle repair

## Summary

PublisherStudio 4.1.0 follows the repeated Windows/DevExpress runtime failure back to the Panel Studio DOM migration instead of adding another render-delay workaround. The reported `componentContentChanged` → `initializeComponent` → null `addEventListener` exception occurs immediately after a blank Panel Studio document is created, while both `PreviewRevision` and `AuthoringRevision` are still zero. That evidence rules out authored-element mutation as the initiating failure.

The supplied PublisherStudio 3.7.6 source is used as the known-good comparison. Its Panel Studio authoring body exposed the real editor DOM directly and had no maintenance-only nested `DxFormLayout` shells. Those shells were introduced later during the DevExpress/Razor maintenance-layout migration and remained in 4.0.9 even after the popup ownership change.

## Fixed

- Removed the eleven maintenance-only nested `DxFormLayout` / `DxFormLayoutItem` / `Template` shells from the live Panel Studio popup body.
- Restored the direct Panel Studio authoring hierarchy used by the known-good 3.7.6 implementation while retaining the newer DevExpress editors, ribbon, context menu, behaviors, reusable-module controls, live-source support and other later features.
- Kept one ordinary component-level `DxFormLayout` owner outside the popup body, matching the maintained architecture used by the other studio components without putting maintenance layout components inside the dynamically changing portal content.
- Kept `.panel-studio-dialog` as the direct `DxPopup.BodyContentTemplate` root; no native/manual dialog was introduced.
- Preserved the 4.0.8 authored-render revision and 4.0.9 empty-to-first-element render refresh. They remain useful downstream authoring invalidation, but are no longer treated as the explanation for the initialization exception that occurs at revision zero.
- Preserved the 4.0.9 prepared DevExtreme asset repair and strict single-file website export integrity checks.

## Why this differs from 4.0.9

4.0.9 removed the FormLayout that owned the popup portal itself. The new runtime evidence shows the same DevExpress initialization exception before any authoring mutation, so that outer ownership hypothesis was insufficient. The remaining significant topology difference from the working 3.7.6 Panel Studio was the set of nested maintenance FormLayouts inside the live popup body. 4.1.0 removes those shells rather than adding timing, retry or JavaScript-error suppression.

## Preserved

- Panel Library documents and their existing rendering/persistence model.
- Publication schema, export model, behaviors, data connections and saved reusable modules.
- DevExpress modal ownership and current component/editor controls.
- Existing Panel Studio interaction binding, geometry, preview presets and authoring revisions.
- The 4.0.9 repaired `dx.light.css` prepared asset and manifest verification.
- No build or maintenance script code changes. The DevExpress retention contract is narrowed only for `PanelStudio.razor` from 186 to 166 tags so the guard continues protecting every remaining functional DevExpress control without forcing the removed maintenance-only wrappers back into runtime DOM.

## Version

`4.0.9 -> 4.1.0`, following the maintained single-digit version-slot rule.
