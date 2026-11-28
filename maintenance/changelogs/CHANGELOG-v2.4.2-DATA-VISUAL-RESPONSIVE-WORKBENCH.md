# PublisherStudio 2.4.2 — Data Visual responsive workbench hardening

## Frontend release checklist

- [x] Canonical publication object behavior is unchanged; this release only adjusts editor layout CSS.
- [x] Selection, move, resize, rotate, duplicate, delete, and layer-order behavior are untouched.
- [x] Mouse/touch/keyboard command routing is untouched; the workbench keeps native independent scroll panes.
- [x] No new global Z-index or competing overlay ownership was introduced.
- [x] Preview, HTML/website, raster/SVG, print/PDF, and video-render pipelines are untouched.
- [x] No listeners, pointer captures, observers, object URLs, or JavaScript interop were added.
- [x] No service or frontend failure boundary changed; existing structured logging/notification behavior remains intact.
- [x] The regression target is narrow-screen editor reachability and independent pane scrolling.

## Changes

- Advanced PublisherStudio and PublisherStudio.InstallerConsole from 2.4.1 to 2.4.2. The version-number rollover rule remains satisfied; neither the minor nor patch slot reaches two digits.
- Preserved the existing Insert/edit data visual workbench layout used as the LocalGPT configuration reference.
- Hardened the Data Visual editor's type/settings panes with explicit minimum sizing, overscroll containment, and stable scrollbar gutters.
- Added a phone/narrow-window layout that stacks the visual-type chooser above the settings pane rather than forcing the desktop workbench into an unusably narrow viewport.
- Repaired the documentation/1-Wire static audit's stale Pages artifact expectation from 2.2.5 to the shipped 2.3.6 documentation artifact.

## Validation boundary

Source-only delivery by request. No `dotnet build`, restore, publish, GitHub access, or online repository access was performed. Static validation details are recorded in `VALIDATION-v2.4.2-source.md`.
