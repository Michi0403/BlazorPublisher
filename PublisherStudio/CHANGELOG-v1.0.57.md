# PublisherStudio 1.0.57

## Uniform editor canvas zoom

- Publication objects now keep a stable 96-DPI internal layout and are scaled as one visual unit when the editor zoom changes.
- RichEdit text frames no longer reflow, enlarge, overlap, or expose different line breaks at 50%, 75%, 125%, or other canvas zoom levels.
- WordArt, spreadsheet previews, shapes, barcodes, data visuals, Professional Components, live sources, video, and native audio controls follow the same visual zoom as pictures.
- Object geometry, connector ports, resize handles, snapping, drag coordinates, and connector routing remain on the existing zoom-aware outer frame, so editing behavior is preserved. Live resize also updates the stable-layout wrapper immediately and restores it on a cancelled pointer operation.
- Text padding, borders, picture masks, shape strokes, and spreadsheet borders are authored at canonical 96-DPI size before the single editor zoom transform is applied, preventing double scaling.
- Print, PDF, image, SVG, website, and standalone publication export continue to use the canonical `PrintPublication` surface and are intentionally unchanged.

Application and installer version: `1.0.57`. Publication format remains `1.45`; picture format remains `1.2`.
