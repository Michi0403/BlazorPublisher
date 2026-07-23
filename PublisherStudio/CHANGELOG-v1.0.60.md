# PublisherStudio 1.0.60

## Deterministic Gallery navigation in the designer

- Gallery navigation buttons and indicators now retain native pointer ownership when the Gallery object is selected. The publication canvas no longer captures the same pointer sequence as a DevExtreme control, so one button press cannot be interpreted as both object movement and Gallery navigation.
- The first control click on an unselected Professional Component remains selection-only. Once selected, buttons, tabs, menu items, form controls, Gallery navigation, sliders, and scrollbars can be operated without starting an object drag.
- Designer Gallery swipe recognition is disabled because the outer publication canvas owns drag gestures. Button navigation settles immediately with no animation overlap, and the selected Gallery index survives harmless component rerenders.
- Website and standalone HTML output retain normal DevExtreme Gallery swipe navigation and animation. Export rendering is unchanged.

## Selectable application zoom rendering

- The mainframe **View > Zoom** ribbon group now exposes two publication-canvas rendering modes:
  - **Sharp CSS layout (default)** rerasterizes text, RichEdit HTML, WordArt, media controls, spreadsheets, charts, and ordinary publication content at the current application zoom when Chromium/Edge CSS zoom is available.
  - **Compact transform** uses the previous transform scaler. It is useful when the user prefers the older, more compact visual behavior or when a browser does not support CSS layout zoom reliably.
- The selected rendering mode is saved with the publication and templates, alongside the other publication view settings. Publication format is updated to `1.47`.
- DevExtreme Professional Components keep the transform-compatible content path even while Sharp CSS mode is selected. This avoids feeding CSS-zoomed pointer coordinates into Gallery, Map, Vector Map, virtual scrolling, and other widget internals while ordinary document content remains sharply rerasterized.
- Live resize and mode switching clear stale inline zoom/transform values and resynchronize every publication object from its authored 96-DPI dimensions.
- PDF, image, SVG, print, website, and standalone HTML export paths remain independent of the editor-only rendering selector.

Application and installer version: `1.0.60`. Publication format: `1.47`; picture format remains `1.2`.
