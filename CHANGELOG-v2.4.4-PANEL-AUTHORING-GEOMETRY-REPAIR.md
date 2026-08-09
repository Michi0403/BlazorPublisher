# PublisherStudio 2.4.4 — Panel authoring geometry repair

## Scope

This release is focused on Panel/Div Studio authoring geometry and DataVisual sizing. It deliberately keeps the existing publication model, panel library, exporters, media features, streaming work, and documentation delivery intact.

## Root causes fixed

- Panel Studio rendered the live panel inside `publication-panel-viewport`, but its selection/hit layer lived outside that viewport on the surrounding editor canvas. Top/side navigation therefore changed the live component coordinate origin without changing the selection origin.
- `ForceCanvasLayout` generated authored X/Y/width/height styles for responsive panels, but the global responsive CSS still overrode those values with relative grid layout. The live object and the hitbox could therefore disagree even when they were fed the same persisted geometry.
- Panel Studio stretched the edited panel to the available dialog rectangle rather than preserving the panel object's authored width/height ratio. This made component dimensions look different from the same panel after insertion into a publication.
- DevExtreme DataVisuals captured concrete `clientWidth/clientHeight` during first render. If a dialog/panel was still being laid out, charts could initialize at pathological dimensions such as a full-page width with a one-pixel height and never recover.
- Blazor parent re-renders recreated unchanged DataVisual widgets, increasing layout churn exactly while Panel Studio selection and resizing were active.

## Changes

- Advanced `PublisherStudio.Web` and `PublisherStudio.InstallerConsole` from 2.4.3 to 2.4.4.
- Added a `PanelView.AuthoringOverlay` contract and render it inside the exact `publication-panel-viewport` that owns element coordinates.
- Moved Panel Studio hitboxes and drag/drop overlays into that viewport, so live elements, selection rectangles, resize handles, and drop coordinates share one geometry owner.
- Browser drop-point conversion now resolves the marked panel authoring viewport instead of using the outer editor canvas.
- Added the explicit `panel-force-canvas` class. Responsive presentation CSS now excludes that class, so Arrange mode really uses the persisted X/Y/width/height canvas geometry for every component kind.
- Panel Studio now preserves the edited panel object's width/height aspect ratio instead of stretching the canvas to arbitrary dialog dimensions.
- Added DataVisual `ResizeObserver` ownership. Visuals defer rendering while their host is degenerate, then size/repaint against the actual host whenever it changes.
- DataVisual teardown now disconnects the resize observer and cancels pending animation frames together with the DevExtreme widget/timer.
- Unchanged DataVisual configurations are no longer recreated on every Blazor parent render; container resizing is owned by the browser observer instead.
- Added `Assert-PanelStudioAuthoringGeometry.ps1` and wired it into the existing pre-build guard chain so a future split between live geometry and authoring geometry is rejected at source-validation time.

## Compatibility

- No publication JSON field was removed or renamed.
- Existing FixedCanvas and Responsive panel documents remain compatible.
- Responsive panels continue using their responsive renderer outside Arrange mode; Arrange mode intentionally shows the authored canvas geometry, while Interact/export retain responsive behavior.
- Exporters continue consuming the same `PanelElement` and child element data.

## Validation boundary

Source-only delivery by request. No `dotnet`, MSBuild, restore, publish, runtime compilation, GitHub access, or online repository access was performed. Static validation details are recorded in `VALIDATION-v2.4.4-source.md`.
