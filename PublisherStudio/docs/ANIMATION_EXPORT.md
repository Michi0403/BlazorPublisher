# Animation and presentation export architecture

## Why the model is renderer-neutral

Animation data belongs to the publication document, not to JavaScript or a specific export format. Each `PublicationAnimation` describes intent:

- phase: entrance, emphasis, motion, or exit;
- semantic effect: fade, fly, float, zoom, wipe, bounce, pulse, spin, shake, grow/shrink, or move;
- trigger: page entry, with previous, after previous, or click;
- timing and easing;
- optional direction, distance, scale, rotation, repetition, and auto-reverse.

The page timeline is defined by `Order` across all objects on that page. This avoids embedding CSS names, browser keyframes, PowerPoint XML, or video-frame data in `.pubstudio.json`.

## Page and object separation

`PublicationPageTransition` handles movement between pages. Object timelines handle what happens inside a page. `PublicationInteraction` handles user actions after an object is clicked. These concerns remain separate so exporters can choose the closest native representation:

- HTML: page shells, Web Animations API, DOM click handlers;
- PowerPoint: slide transitions, timing tree, click/with/after triggers, hyperlinks/actions;
- video: deterministic page/timeline scheduler rendered at a chosen frame rate;
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

A video exporter should consume the normalized page timeline rather than recording the editor UI. For each page it can:

1. determine the page-transition interval;
2. resolve automatic groups and configured click groups using an export policy;
3. evaluate element transforms, opacity, clipping, and visibility at each timestamp;
4. rasterize at the requested resolution and frame rate;
5. send frames to an encoder such as FFmpeg in an optional export service.

Click interactions require an export policy because video is not interactive. Reasonable policies are: play all click groups in order with a configurable pause, ignore navigation actions, and optionally render URL/action hints.

## Current implementation boundary

The current package implements the document model, editor, preview engine, interaction authoring, and animated self-contained HTML export. It does not yet write PowerPoint timing XML or encode video. Those exporters can be added without changing the publication format or editor timeline.
