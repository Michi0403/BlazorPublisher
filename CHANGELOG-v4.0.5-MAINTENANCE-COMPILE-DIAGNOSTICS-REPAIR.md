# PublisherStudio 4.0.5 — maintenance compile and diagnostics repair

## Summary

PublisherStudio 4.0.5 repairs the maintenance/build failures reported against 4.0.4 while retaining the 4.0.4 DevExpress-first editor work and Panel Studio structural-renderer recovery. The fixes are in application/component source; maintenance scripts and policy checks are not weakened to make the errors disappear.

## Razor compiler repair

- `SystemFontPicker` now specifies `TData="string"` and `TValue="string"` on its DevExpress `DxComboBox`, removing the generic type-inference failure while preserving installed-font data, editable custom font-family text, search and virtual list rendering.
- `SystemFontPicker` now composes its required and optional CSS classes in a component property and passes the result as one Razor expression, eliminating the reported mixed-content `CssClass` error.
- `DevExpressColorPicker` received the same parser-safe CSS-class composition proactively so the equivalent newly introduced mixed-content attribute cannot become the next Razor compile failure.

## Component diagnostics repair

- `DevExpressColorPicker` now publishes a user-visible error notification when one of its guarded operational methods fails, in addition to its existing structured logging and exception propagation. This satisfies the maintained new-component catch/log/notification boundary without changing the diagnostics audit.

## Application static-policy repair

- Page Appearance & Effects enum option collections are now component-owned `IReadOnlyList<T>` instance properties rather than `private static readonly` arrays.
- Panel Studio navigation, layout, behavior, visual, component, chat and live-source option collections are likewise component-owned instance properties.
- The enum values and DevExpress bindings are unchanged; only ownership was corrected to comply with the existing application-static architecture policy.

## Compatibility

No publication schema, persisted document data, wire contract, Panel Studio authored geometry or LocalGPT code is changed in this release.
