# PublisherStudio 3.9.0 source validation

Source-only validation was performed without invoking `dotnet`, MSBuild, NuGet restore, publish, GitHub, or other online repository access.

Checked in this handoff:

- Restored Mainframe/ruler markup and the 3.8.9 box-neutral DevExpress maintenance-wrapper contract remain present.
- Data Visual chooser CSS explicitly owns readable text/background states; `.check-row` overrides the generic visual-settings label grid.
- Rectangle corner-radius rendering is present in PageSurface, PanelView, and PrintPublication.
- Website export popup dimensions are bounded and their inner dialog owns the popup body geometry/internal scrolling.
- Presentation runtime creates previous/replay/counter/next/full-screen controls and defaults them on unless `data-playback-controls="false"`.
- `publisherInterop.js` parses with Node syntax checking and its maintained SHA-256 diagnostics entry was refreshed.
- Version/cache metadata is 3.9.0, rolling over from 3.8.9 instead of creating 3.8.10.
- InteractiveServer render-mode directives were not edited.

A real compiler/build result is intentionally not claimed; the user's build environment remains authoritative.
