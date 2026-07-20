# Animation and presentation export architecture

## Why the model is renderer-neutral

Animation data belongs to the publication document, not to JavaScript or a specific export format. Each `PublicationAnimation` describes intent:

- phase: entrance, emphasis, motion, or exit;
- semantic effect: fade, fly, float, zoom, wipe, bounce, pulse, spin, shake, grow/shrink, or move;
- trigger: page entry, with previous, after previous, or click;
- timing and easing;
- optional direction, distance, scale, rotation, repetition, and auto-reverse.

The default animation sequence is defined by `Order` across all objects on the page. An optional `TimelineStartSeconds` overrides that trigger-derived position when a clip is placed directly on the visual page timeline. This avoids embedding CSS names, browser keyframes, PowerPoint XML, or video-frame data in `.pubstudio.json`.

## Page and object separation

`PublicationPageTransition` handles movement between pages. Object timelines handle what happens inside a page. `PublicationInteraction` handles user actions after an object is clicked. `PublicationMediaElement` adds embedded source data, trim range, page-timeline start, playback rate, volume, fades, looping, and semantic playback trigger. These concerns remain separate so exporters can choose the closest native representation:

- HTML: page shells, Web Animations API, native `<audio>` / `<video>`, and DOM click handlers;
- PowerPoint: slide transitions, timing tree, click/with/after triggers, hyperlinks/actions, and native media relationships where supported;
- video: deterministic animation/media scheduler rendered and mixed at a chosen frame rate;
- static PNG/JPEG/SVG/PDF: final publication appearance with no playback runtime.

## HTML runtime

The website exporter clones the existing print surface, preserves animation and interaction metadata as `data-*` attributes, and injects a dependency-free playback runtime. It supports:

- responsive page scaling;
- incoming page transitions;
- automatic and click-triggered timeline groups;
- repeat and auto-reverse;
- previous, next, replay, fullscreen, keyboard, loop, and auto-advance controls;
- navigation, URL, visibility, and replay interactions;
- elements hidden at presentation start;
- embedded audio/video with timeline start, trim range, fades, rate, volume, loop, page-entry/click playback, and media interaction commands;
- print and reduced-motion fallbacks.

The runtime only opens URLs with `http`, `https`, or `mailto` schemes.

## PowerPoint mapping target

A PowerPoint exporter can use one slide per `PublicationPage` and one shape per `PublicationElement`. The semantic mapping is direct for common effects:

- Fade → fade entrance/exit;
- Fly/Float → directional entrance/exit;
- Zoom → grow/shrink entrance/exit;
- Wipe → directional wipe;
- Pulse/GrowShrink/Spin → emphasis;
- Move → motion path or translated end state;
- OnPageEnter/WithPrevious/AfterPrevious/OnClick → the corresponding timing-node trigger.

Features without an exact PowerPoint equivalent should be reduced predictably rather than silently discarded. For example, a browser `BackOut` easing can map to the closest acceleration/deceleration values.

## Video mapping target

A video exporter should consume the normalized page timeline rather than recording the editor UI. The same timeline now contains animation clips and non-destructive media clips. For each page it can:

1. determine the page-transition interval;
2. resolve automatic groups and configured click groups using an export policy;
3. evaluate element transforms, opacity, clipping, and visibility at each timestamp;
4. decode, trim, rate-adjust, fade, and mix embedded audio/video sources at their semantic timeline positions;
5. rasterize at the requested resolution and frame rate;
6. send frames and mixed audio to an encoder such as FFmpeg in an optional export service.

Click interactions require an export policy because video is not interactive. Reasonable policies are: play all click groups in order with a configurable pause, ignore navigation actions, and optionally render URL/action hints.

## Current implementation boundary

The current package implements the document model, animation/media timeline editor, browser-native media studios, preview engine, interaction authoring, and animated self-contained HTML export. It does not yet write PowerPoint timing XML/native media packages or encode a final video. Those exporters can be added without changing the publication format or editor timeline.

## Offline Signal Arrows and Signal Connectors (v1.0.41)

A signal connector is a normal publication connector plus a serialized `SignalConnectorSettings` payload. Its endpoints may reference an object anchor or a page coordinate. Optional endpoint selectors target an element inside the referenced object, for example `[data-cell='B4']` in a spreadsheet preview.

The browser runtime is shared by editor preview, presentation/site HTML, and video export. It supports page-entry, click, hover, and manual triggers; flying-arrow, draw-path, pulse, and invisible visuals; start/end click or hover gestures; translate/scale/rotate/opacity motion; animated visibility and opacity; highlighting; animation replay; media actions; CSS classes; and signal chaining.

Single-file HTML exports embed the runtime function and the serialized settings directly. No call back to PublisherStudio is made. The exported document therefore remains interactive when opened from local storage with no network. A publication that separately uses REST/OData or other remote live data still needs access to those configured endpoints.

Video export runs the same runtime against the recording DOM. Chained signal durations are included in the page recording duration. Infinite signal loops are recorded as their configured finite repeat count so export termination remains deterministic.

## Reversible preview state and signal resizing (v1.0.42)

Editor preview is treated as a temporary playback session. Before an ordinary animation or signal mutates an object, the browser records the relevant DOM state. **Stop preview** cancels active Web Animations and signal chains, removes transient runners, and restores transforms, width/height, opacity, inline style, classes, hidden state, and media playback position. Ordinary animation preview also restores automatically after its preview sequence completes.

Signal motion now has two distinct size mechanisms:

- `Scale` applies a transform scale without changing layout dimensions.
- `ResizeWidthPercent` and `ResizeHeightPercent` animate the target's measured box width and height. A value of `100` leaves that dimension unchanged.

Click and hover triggers are delegated from the publication root and resolve their current endpoint target for every event. This avoids stale listeners when a DevExtreme component, spreadsheet preview, chart, or custom HTML subtree replaces its internal DOM. A mutation observer separately maintains connector-line hit testing.

Page-entry playback calls signal reset before evaluating its triggers. Replaying or re-entering a page therefore uses the current publication objects as a stable initial baseline instead of compounding a previous preview run. The runtime implementation is embedded directly in offline HTML exports, including its reset and resize behavior.
