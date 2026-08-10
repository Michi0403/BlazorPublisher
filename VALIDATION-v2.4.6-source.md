# PublisherStudio 2.4.6 source validation

This package is source-only and was intentionally not compiled with .NET/MSBuild in this environment.

## Version

- PASS — PublisherStudio.Web version is 2.4.6.
- PASS — PublisherStudio.InstallerConsole version is 2.4.6.
- PASS — documentation source version is 2.4.6.

## Panel Studio lifecycle regression

- PASS — interaction binding identity is `_interactionBindingId` only.
- PASS — binding identity does not include CanvasWidth, CanvasHeight, PanelDesignWidth or PanelDesignHeight.
- PASS — panel design dimensions are tracked separately through `designSurfaceLayoutKey`.
- PASS — design-size changes invoke `publisherStudio.refreshPanelStudioDesignSurface` without tearing down pointer/keyboard/context-menu/gamepad binding.
- PASS — arrange/interact mode and preview refresh retain the browser binding.
- PASS — the maintained PowerShell lifecycle guard was strengthened to enforce this separation.
- PASS — exact-equivalent lifecycle guard patterns were executed locally against the source and passed. PowerShell itself is not available in this environment.

## Existing authoring geometry contract

- PASS — PanelView still owns the authoring overlay/coordinate viewport.
- PASS — Panel Studio design size still derives from panel-local CanvasWidth/CanvasHeight rather than Mainframe Width/Height.
- PASS — responsive presentation rules remain isolated from forced authoring canvas mode.
- PASS — DataVisual host-size observation and degenerate-size rejection remain present.
- PASS — exact-equivalent Panel Studio authoring-geometry guard patterns passed.

## Static checks executed

- PASS — `node --check` for `publisherInterop.js`.
- PASS — JavaScript diagnostics SHA-256 manifest refreshed for `publisherInterop.js`.
- PASS — application architecture audit (`publisherstudio`, all modes).
- PASS — service resilience audit.
- PASS — documentation / 1-Wire contract audit.

## Not executed

- .NET restore/build/publish/runtime compilation was not executed.
- GitHub or online repository access was not used.
