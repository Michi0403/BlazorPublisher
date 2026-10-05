# PublisherStudio 4.0.4 — DevExpress UI and Panel renderer repair

## Summary

PublisherStudio 4.0.4 repairs two regressions exposed after the broad DevExpress layout migration: geometry-sensitive Panel Studio/render components had acquired maintenance `DxFormLayout` wrappers that changed the authored DOM hierarchy, and several visible editor workflows had fallen back to native HTML controls even though equivalent DevExpress Blazor editors exist.

## Panel Studio / renderer repair

- Restored `PanelView` to a direct authored `publication-panel` root and direct `publication-panel-view` children. The Panel Studio design frame, fixed-canvas sizing, element percentage geometry, hit-test overlay and export-equivalent panel DOM once again share the same containing-block hierarchy.
- Marked geometry-sensitive render leaves with the explicit `razor-structural-renderer` contract instead of inserting artificial maintenance FormLayouts around them. This applies to PanelView and its geometry-sensitive DataVisual, DevExtreme, media, HTML, barcode and effect rendering leaves.
- Kept meaningful DevExpress controls inside those renderers. The exception is only about wrapper ownership around authored/runtime geometry, not a retreat from DevExpress UI.
- Strengthened `Assert-PanelStudioAuthoringGeometry.ps1` and `audit_razor_maintenance_contract.py` so future maintenance work cannot silently put semantic-layout wrappers back around structural render roots.
- Kept the two Panel Studio native buttons that are browser-owned geometry primitives only: the draggable palette source and authored hit-test/resize surface. Their reason is documented in source.

## DevExpress-first UI repair

- Replaced `SystemFontPicker`'s native input/select/datalist stack with a searchable, virtualized `DxComboBox` that allows both installed-font selection and custom font-family text.
- Added reusable `DevExpressColorPicker`, built from `DxDropDownBox` and `DxColorPalette`, and migrated maintained PublisherStudio native color inputs to it.
- Reworked Page Appearance & Effects to DevExpress-native buttons, text boxes, check boxes, combo boxes, spin edits and color palettes. The reported native-looking page-effects dialog no longer owns its visible editor controls through raw HTML inputs/buttons.
- Replaced maintained datalist-based URL/media encoder/preset/pixel-format suggestions with searchable DevExpress combo boxes while retaining custom-entry behavior.
- Converted a substantial set of Panel Studio authoring/inspector controls to DevExpress editors, buttons and accordions while preserving browser-owned drag/drop and coordinate hit-testing boundaries.

## Maintenance contract

- `Assert-DevExpressComponentRetention.ps1` now counts Razor markup rather than documentation/code text and separately tracks DevExpress tags, native interactive controls and native disclosure (`details`/`summary`) debt per component.
- New native `datalist` and `input type="color"` usage is build-breaking. Diagnostics state the intended replacements: searchable `DxComboBox`/`DxListBox`/`DxSearchBox` and `DxColorPalette` (optionally hosted by `DxDropDownBox`).
- Existing native disclosure debt may only decrease; new collapsible UI should use `DxAccordion` or another appropriate DevExpress navigation/flyout control.
- `AGENTS.md` now distinguishes ordinary DevExpress-owned editor layout from structural render surfaces whose direct DOM hierarchy is part of their geometry/runtime contract.

## Compatibility

The publication model and saved panel data are unchanged. This release repairs rendering/editor ownership without introducing a document-format migration.
