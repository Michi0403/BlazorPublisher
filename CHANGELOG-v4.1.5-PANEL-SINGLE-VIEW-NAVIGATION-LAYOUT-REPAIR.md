# PublisherStudio 4.1.5 — Panel single-view navigation layout repair

## Summary

Panel Studio scratch panels now keep their authored viewport instead of collapsing it when only one view exists. The supplied runtime/project/export evidence shows the element graph was valid; the rendered viewport was zero-height.

## Fixed

- `PanelView` now distinguishes the configured navigation mode from whether a navigation surface is actually rendered.
- When navigation is hidden or there is only one enabled view, `panel-nav-suppressed` collapses the panel grid to one row and one column so the viewport owns the full panel.
- This also protects single-view `SideMenu` configurations from retaining an unused navigation column.
- Multi-view panels with visible TopTabs/SideMenu/OverlayMenu retain their existing navigation layout.
- Added `data-panel-navigation-rendered` for direct browser diagnostics without changing the serialized navigation mode.

## Evidence

The supplied 4.1.4 project contains `Panel 3` with one enabled `Home` view, `navigationMode: TopTabs`, and visible authored elements. Its supplied single-file HTML snapshot records `grid-template-rows: 0px 253.781px`, a `.publication-panel-viewport` height of `0px`, and a zero-sized canvas region. The Panel Library's `Live KPI Dashboard 4` uses `navigationMode: Hidden` and receives the full viewport. This explains why the scratch element list, persistence, and export model contained the objects while the Panel Studio canvas appeared empty.

## Preserved

- DevExpress/DevExtreme remains **25.2.10**.
- .NET remains **10.0.12**.
- Automatic DevExpress asset provisioning from 4.1.4 remains unchanged.
- Panel Studio lifecycle diagnostics and explicit visibility behavior remain intact.
- The DevExpress `addEventListener` diagnostic remains observable and is not suppressed; it is treated separately because the supplied working Panel Library path also exhibits that lifecycle diagnostic.

## Version

`4.1.4 -> 4.1.5`.
