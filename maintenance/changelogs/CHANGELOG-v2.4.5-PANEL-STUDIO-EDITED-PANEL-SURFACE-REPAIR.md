# PublisherStudio 2.4.5 — Panel Studio edited-panel surface repair

## Fixed

- Panel Studio now sizes its authoring surface from `PanelElement.CanvasWidth` / `CanvasHeight`, which own the coordinates of panel descendants. The outer `PanelElement.Width` / `Height` remain publication/Mainframe placement dimensions and no longer determine the Panel Studio canvas.
- The Panel Studio fit routine is now **shrink-only**. A normal 160 × 90 panel is displayed at its native 96-DPI authoring size when the workspace has room instead of being enlarged until it resembles the publication canvas.
- The workspace outside the selected panel is now a neutral stage. The subtle arrangement grid lives on the selected panel viewport, making the actual edited panel visually explicit.
- The active panel name, local canvas dimensions and panel id are exposed in the authoring surface for diagnostics and the mode banner explicitly states that Mainframe placement remains separate.
- Editing `CanvasWidth` / `CanvasHeight` now invalidates the browser layout key so the panel is immediately re-fitted without requiring a window resize or reopen.
- Existing shared-viewport hitbox, drag, resize and live-element wiring from 2.4.4 is retained. The working camera move/resize path therefore remains on the same coordinate surface.

## Regression guard

`build/Assert-PanelStudioAuthoringGeometry.ps1` now rejects a return to Mainframe `Width` / `Height` for the Panel Studio design surface and requires the browser fit scale to cap at 1.0.

## Version

- `PublisherStudio.Web`: 2.4.5
- `PublisherStudio.InstallerConsole`: 2.4.5

Source-only delivery. No .NET/MSBuild restore, build or publish was performed.
