# PublisherStudio v1.0.11 object workflow

- Added **Snap in objects** as a separate snapping option alongside grid, guide, page, and object snapping.
- Added dynamic internal snap points at useful percentages of the target object. The available precision adapts to the target's on-screen size while always retaining 0%, 25%, 50%, 75%, and 100% anchors.
- Made internal snapping grab-aware: dragging near an object's edge prioritizes that edge, while dragging near its center prioritizes center alignment.
- Added independent horizontal and vertical internal snapping, percentage labels, target highlighting, and snap hysteresis to prevent flicker between nearby points.
- Added Ctrl/Command- and Shift-click multi-selection on the canvas and in the Layers pane. Dragging any selected object moves the complete selection while keeping existing page, guide, grid, and object snapping active.
- Added persistent **Group** and **Ungroup** commands to the Home ribbon and object context menu. Group membership is stored in the publication and is restored when the file is opened again.
- Added a compact drag-to-canvas insert strip for text, pictures, and video. Text is created directly at the drop point; picture and video drops reuse the existing file picker and Media Studio workflows and place the result at the requested position.
- Prevented a completed drag-to-canvas insert from also triggering the button's normal click action.
- Updated the publication format marker to 1.11 while keeping older publications loadable.
- Kept installer, barcode, media editing, and export implementations outside this change.
