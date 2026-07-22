# PublisherStudio 1.0.56

## Restartable animation preview

- Preview startup now clears the previous run before capturing any animation baseline.
- Element and group-member transforms are captured from their stable inline publication transform instead of a currently sampled Web Animations transform.
- Repeated Preview clicks therefore restart from the authored state instead of multiplying translate, scale, rotate, or loop transformations.
- Timeline playback uses the same stable transform fallback.

## Publication-owned editor settings

- Zoom, ruler unit, grid visibility, guides, snapping modes, grid spacing, and export DPI remain part of the publication document and now mark the publication as modified when changed.
- Saved publications and templates carry those standard editor settings to another PublisherStudio installation, providing the same canvas setup for each user.
- Streaming configuration is deliberately excluded from publication JSON and recovery/undo snapshots. Existing embedded streaming settings are migrated once into a local per-publication store.
- The local streaming-settings store uses ASP.NET Core Data Protection and Local Application Data, so provider routing, recording paths, LAN options, and hotkeys are not transferred with a shared publication.

## Stable Professional Components map movement

- Designer-mode `Map` components receive a transparent input shield above the live map surface.
- The publication canvas still receives capture-phase selection, movement, double-click, and context-menu gestures, while DevExtreme and map providers no longer interpret the same drag as a pan.
- Presentation and exported publications remain interactive because the shield is only installed in designer mode.
- `VectorMap` behavior is unchanged.

Application and installer version: `1.0.56`. Publication format remains `1.45`; picture format remains `1.2`.
