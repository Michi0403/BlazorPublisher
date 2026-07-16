# BlazorPublisher v0.8 desktop context menus

## Context menus where editing happens

- Picture Studio now opens a DevExpress context menu from either the canvas or the layer list.
- Right-clicking a Picture Studio canvas location first hit-tests and selects the layer under the pointer, matching the publication canvas behaviour.
- Layer-specific commands are available for raster replacement, fit and flips, paint tools and stroke clearing, procedural render kinds, shape kinds, fill kinds, ordering, placement, adjustments, visibility, and locking.
- Right-clicking empty Picture Studio space provides paste, insert, and view commands.
- The existing **Replace** raster command now replaces the selected raster layer in place instead of adding another image layer; position, size, effects, and ordering are retained.
- The data-visual editor live preview now has a context menu for applying, refreshing, managing data, changing visual type, toggling applicable display options, and resetting visual options.

## Publication-canvas improvements

- The existing publication context menu now starts with Undo and Redo.
- Selected data visuals can be edited, converted between supported visual types, refreshed, and have relevant title, legend, and label options toggled directly.
- Selected shapes can change shape type directly.
- Empty-page context menus now expose the full data-visual and shape catalogues, page duplication/deletion, and the existing insert commands.
- Existing text, picture, WordArt, connector, arrange, alignment, visibility, and locking commands remain in the same DevExpress menu style.

## Desktop interaction in Picture Studio

- Picture Studio gains an internal layer clipboard with Copy and Paste commands in both the ribbon and context menu.
- The focused canvas supports Ctrl/Cmd+Z, Shift+Ctrl/Cmd+Z, Ctrl/Cmd+Y, Ctrl/Cmd+C, Ctrl/Cmd+V, Ctrl/Cmd+D, Delete, Home, End, and Escape.
- Secondary-button pointer events no longer start a paint stroke or transform operation.
- Focus outlines and contextual hints make the keyboard/right-click interaction discoverable without changing the existing visual language.

## Compatibility

- No publication or Picture Studio document schema changed.
- No package, host, installer, route, or licensed dependency changed.
- The PNG chunk-transfer workaround used by **Insert into publication / Apply to picture** remains unchanged.
