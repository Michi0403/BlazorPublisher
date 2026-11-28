# PublisherStudio 4.0.7 — DevExpress/Razor compiler repair

## Summary

PublisherStudio 4.0.7 is a focused maintenance follow-up to 4.0.6. It repairs the compiler failures reported after the 4.0.6 text-service ownership fix while preserving the existing editor behavior and maintenance boundaries.

## Compiler repairs

- `DataManager` now supplies explicit `TData="string"` and `TValue="string"` arguments for the editable monolith-route `DxComboBox`.
- `MediaConverterStudio` now supplies explicit string generic arguments for editable video/audio codec, encoder-preset, and pixel-format `DxComboBox` controls.
- `PanelStudio` prepares selected-list CSS class values in ordinary Razor code before passing them to `DxButton.CssClass`, avoiding mixed literal/C# component attributes while preserving the same selected-state styling.
- `PageEffectStudio.CloseButtonAttributes` now uses the concrete `Dictionary<string, object>` type required by the DevExpress `DxButton.Attributes` contract in this project.

## Compatibility

No publication schema, persisted document format, editor workflow, DevExpress control ownership, LocalGPT code, or maintenance/build script is changed in this release. The 4.0.6 `PanelStudioTextService` ownership repair is retained unchanged.
