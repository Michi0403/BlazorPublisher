# PublisherStudio 1.0.45

## Vertical Menu layout

- Fixed vertical Menu items being generated correctly but placed one full component-height apart by DevExtreme's shared menu wrapper rule.
- Vertical PublisherStudio menus now use natural item heights, full-width rows, normal wrapping, and an internal scrollbar only when their content exceeds the publication object.
- Removed the vertical menu centering pseudo-element that could add an unnecessary empty component-height after the menu rows.
- The correction applies to Component Studio preview, the Publisher canvas, presentation export, and offline website export.

## Component layout resilience

- Added normalized orientation handling for Menu, Tile View, Splitter, and Scroll View.
- Every browser-native component now observes its rendered host size and requests `updateDimensions()`/`repaint()` when the publication object is resized or becomes visible.
- Tab Panel and Multi View refresh nested controls after selection changes.
- Splitter refreshes nested controls after resize, collapse, and expansion.
- Nested panel hosts now retain valid minimum dimensions so grids, schedulers, maps, vector maps, galleries, and other controls do not remain measured at a former hidden size.
- Re-rendering or changing a component kind clears stale DevExtreme host classes, ARIA state, and widget dimensions before the new control is initialized.
- Large generated Forms now scroll inside their publication object instead of silently clipping later fields.

## Validation

- Component runtime syntax and Node contract tests pass.
- Timeline and offline signal/SVG/path regression suites pass unchanged.
- CSS regression assertions cover vertical Menu row height and centering removal.
- Application/package/installer version updated to `1.0.45`; publication format remains `1.42`.
