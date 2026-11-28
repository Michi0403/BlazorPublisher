# PublisherStudio 4.1.5 source validation

## Scope

Source-only repair of the single-view panel navigation layout. No .NET build, restore, publish, GitHub access, or licensed DevExpress asset generation was performed in this environment.

## Runtime evidence

- The supplied project contains scratch-created `Panel 3` with `navigationMode: TopTabs`, one enabled view, and visible authored elements.
- The supplied HTML snapshot records the scratch panel root with computed rows `0px 253.781px`, while `.publication-panel-viewport` is `0px` high and the canvas region is zero-sized.
- The supplied Panel Library `Live KPI Dashboard 4` uses `navigationMode: Hidden` and receives a non-zero full-height viewport.
- Runtime logs show scratch creation succeeded and later authored revisions changed, so the blank canvas is not missing model data.
- The DevExpress `addEventListener` diagnostic remains a separate lifecycle issue because it also appears around a working Panel Library path.

## Repair contract

- `PanelView` adds `panel-nav-suppressed` whenever no navigation surface is rendered.
- Suppressed/hidden navigation uses one grid row and one grid column.
- The configured navigation mode remains unchanged in the document and `data-panel-navigation`.
- `data-panel-navigation-rendered` exposes the effective rendered state.
- Multi-view visible navigation retains its existing layout.

## Dependency baseline

- DevExpress/DevExtreme remains **25.2.10**.
- .NET remains **10.0.12**.
- Automatic DevExpress asset provisioning from 4.1.4 remains unchanged.

## Static checks

Architecture, async continuation/boundary, component resilience, prerender JavaScript interop safety, Razor maintenance architecture, service resilience, transient UI-state ownership, Panel Studio persistence, cross-platform boundaries, XML documentation, and text-service ownership were checked from source. The `build/` tree is unchanged from 4.1.4.

The actual Blazor/DevExpress runtime build remains for the licensed Windows development machine.
