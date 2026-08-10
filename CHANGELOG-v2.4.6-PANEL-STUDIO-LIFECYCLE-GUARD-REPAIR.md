# PublisherStudio 2.4.6 — Panel Studio interaction lifecycle guard repair

## Fixed

- Restores the Panel Studio browser-interaction lifecycle contract that the 2.4.5 authoring-surface change accidentally violated.
- Keeps `_interactionBindingId` as the only binding identity for pointer, keyboard, context-menu and gamepad interop.
- Separates panel-local canvas sizing from browser-handler lifetime through a dedicated design-surface layout key.
- CanvasWidth/CanvasHeight changes now call `refreshPanelStudioDesignSurface` instead of forcing an interaction rebind.
- Retains the 2.4.5 edited-panel surface behavior and the 2.4.4 shared authoring viewport / DataVisual resizing fixes.
- Strengthens `Assert-PanelStudioInteractionLifecycle.ps1` so authoring dimensions are explicitly forbidden from entering the binding key and a separate layout-refresh path is required.

## Build guard

The guard must remain enabled. PublisherStudio 2.4.5 used a binding key containing `PanelDesignWidthPx` and `PanelDesignHeightPx`; the maintained lifecycle guard correctly rejected that regression. 2.4.6 fixes the source rather than bypassing the guard.

## Version

- `PublisherStudio.Web`: 2.4.6
- `PublisherStudio.InstallerConsole`: 2.4.6
- LocalGPT 1-Wire protocol package: unchanged
