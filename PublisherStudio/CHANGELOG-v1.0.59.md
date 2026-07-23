# PublisherStudio 1.0.59

## Sharp high-DPI editor zoom

- Canvas content now uses Chromium/Edge CSS layout zoom when available instead of permanently scaling a low-resolution composited layer. Text, RichEdit HTML, WordArt, native media controls, spreadsheets, charts, and Professional Components are rerasterized at the current editor zoom on UHD displays.
- The authored 96-DPI content size remains stable, so text wrapping and component geometry do not change when zoom changes. The previous transform path remains as a standards-compatible fallback.
- Permanent transform-layer promotion was removed from ordinary publication content and is retained only while an inner content-pan operation needs it.
- Print, PDF, image, SVG, website, and standalone HTML export paths are unchanged.

## Provider-safe Map configuration

- New Map objects no longer default to a keyless Google provider. Component Studio requires a supported provider and API key before Apply is enabled.
- The runtime checks the provider configuration before DevExtreme is initialized. Missing or invalid configuration renders a local placeholder and performs no external provider request. Older keyless maps therefore fail closed instead of loading Google automatically.
- Component Studio exposes Google, Google Static, Azure, and legacy Bing contracts, plus the optional Google Map ID used for advanced markers. Provider names and keys are normalized during publication load.
- A one-click command converts the draft to the bundled, keyless Vector Map. OpenStreetMap is not silently injected into DevExtreme Map because it is not a supported dxMap provider contract and the public OSM tile service is not an offline/commercial default backend.
- Standalone browser exports necessarily contain any configured browser-side map key; the editor warns users to apply provider/API/origin restrictions.

## Stable map viewport controls

- Provider Map and Vector Map center/zoom changes are accepted only after the widget is ready and a real pointer, touch, wheel, or control-bar gesture has started. Provider initialization and auto-fit changes are ignored.
- Publication persistence waits until pointer/touch release or wheel idle, then commits one final viewport. This removes the previous mid-gesture rerender loop that made the Vector Map zoom bar jump or require repeated over-pulling.
- Pending viewport and gesture listeners are cancelled during rerender/disposal. Vector Map persistence now preserves its native `1..256` zoom-factor range instead of truncating every committed value to the provider Map `1..20` range. Existing explicit **Move map object** and **Pan / zoom map content** ownership remains unchanged.

## Real-data chart mapping

- Publication chart settings now persist argument-axis mode (`Auto`, `Discrete`, `Continuous`, or `DateTime`), repeated-category aggregation, and point ordering. Publication format is updated to `1.46`.
- Chart Studio adds **Auto-map fields**, a live Mapping check, numeric-measure filtering/guidance, and explicit controls for aggregation and sorting.
- Auto-map assigns sensible roles for ordinary category/time charts, XY/bubble data, OHLC/stock data, Sankey source/target/weight data, and TreeMap label/parent/value data. Existing manual role selectors remain available.
- Browser preview and export use typed arguments, per-series/category aggregation (`sum`, `average`, `minimum`, `maximum`, `count`, or no aggregation), and deterministic point sorting. Specialized range, bubble, and financial rows remain explicit.
- Range Selector scale configuration uses its native `valueType` contract while Chart/Polar Chart use argument-axis typing.

## Larger creative workspaces

- Story/RichEdit, Spreadsheet, spreadsheet-data selection, Data Manager, Chart Studio, Picture Studio, Media Studio, Streaming Studio, Barcode Studio, and Component Studio now fill the available browser viewport.
- One responsive shadow-gap variable is applied equally on every side, including narrow screens, instead of leaving large unused side margins while editor text and controls are clipped.

Application and installer version: `1.0.59`. Publication format: `1.46`; picture format remains `1.2`.
