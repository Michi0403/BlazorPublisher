# PublisherStudio v1.0.41 — SVG paths and offline Signal Arrows

## Picture Studio

- Added **Download SVG** to the Output menu.
- SVG output is self-contained and keeps supported text, fill, shape, and path layers as SVG elements. Raster, paint, and procedural render layers are embedded per layer so the file remains portable and visually complete.
- Added a **Path** drawing tool for freehand vector paths.
- Path layers support open/closed contours, smoothing, fill/stroke settings, exact point X/Y editing, point insertion/removal, and direction reversal.
- Picture document format is now `1.2` and normalizes stored path nodes safely.

## Signal Arrow / Signal Connector system

- Added Signal Arrow and Signal Connector drawing tools. Endpoints can attach to publication objects or any exact page position.
- Each signal can be visible, hidden, a flying arrow, a drawn path, a pulse, or action-only.
- Triggers include page entry, click, hover, and manual playback.
- Start and end points can emit click or hover gestures against the endpoint object or an inner CSS selector.
- Signals can transform an object or its inner content while travelling: translate, scale/zoom, rotate, and opacity. Map, vector-map, spreadsheet, and text content viewports are selected automatically when no inner selector is supplied.
- Completion actions include click, hover, animated show/hide/toggle, animated opacity, animation replay, media playback control, highlighting, CSS class changes, and starting another signal.
- Signals can be chained to build ordered map movements, spreadsheet-cell walkthroughs, chart highlights, popups, and multi-step explanations.
- Spreadsheet preview cells now expose stable selectors such as `[data-cell='B4']`, plus row and column attributes.

## Offline HTML, streaming, and video

- The complete signal runtime and signal configuration are embedded into both single-file HTML export modes.
- Exported signal actions, selectors, animation timing, and chaining work without a PublisherStudio server and without a network connection.
- Existing remote live-data sources still require network access when the publication itself is offline; local signal behavior does not.
- Presentation and website page navigation dispatch page-entry signals.
- Video export runs the same signal engine, includes chained signal durations, and converts endless loops into finite export cycles so recording can finish.
- Synthetic click/hover events are marked and ignored by trigger listeners, preventing a signal from recursively retriggering itself.

## File formats and version

- Application/package/installer version: `1.0.41`.
- Publication document format: `1.39`.
- Picture document format: `1.2`.
