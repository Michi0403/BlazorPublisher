# v0.9 — publication animations, transitions, and interactive website playback

## Animation model

- The publication format advances to `1.8`.
- Every publication element can own an ordered list of animation steps.
- Animation steps are independent of the renderer and store phase, effect, trigger, easing, direction, duration, delay, distance, scale, rotation, repeat count, and auto-reverse.
- The page owns its incoming transition, click/automatic advance policy, and timing.
- The document owns presentation playback settings for automatic start, looping, and visible controls.
- Every element can also carry a click interaction: page navigation, URL opening, visibility changes, or animation replay.
- Elements can be marked **Hidden when the page starts**, allowing another object to reveal them during playback.

## Editor experience

- A fourth inspector tab provides a page-wide animation timeline while retaining normal object selection.
- Entrance, emphasis, motion, and exit presets are available from both the inspector and the new **Animations** ribbon tab.
- Timeline steps can be selected, reordered, renamed, deleted, and edited in detail.
- Page transitions and automatic page advance are edited separately from in-page object animation.
- Page, object, and individual-step previews run directly on the publication canvas.
- The publication context menu gains object-animation presets, preview/removal commands, page preview, and page-transition commands.
- Live duration/delay/distance/scale/rotation sliders create one undo entry per gesture rather than one per pointer update.

## Animated website export

- Website export is now a self-contained, full-window presentation rather than a static stack of pages.
- It includes responsive page fitting, keyboard navigation, optional playback controls, fullscreen, replay, looping, click-triggered animation groups, page transitions, automatic advance, and reduced-motion support.
- Click interactions work for text, pictures, shapes, WordArt, connectors, and data visuals.
- The exported file remains one HTML file and requires no external animation library or web service.
- Printing the exported website still expands every page and hides playback controls.

## Portability

The animation model intentionally stores semantic effects rather than browser keyframes. The HTML exporter maps those effects to the Web Animations API. A future PowerPoint exporter can map the same phases, triggers, directions, and timings to Open XML animation nodes, while a video exporter can render the same normalized timeline frame by frame. The current package implements animated HTML playback; PowerPoint animation writing and encoded video output are not included yet.

## Compatibility and safety

- Older files load with empty object timelines, default page transitions, and default playback settings.
- Animation IDs and page-wide order are normalized on load.
- Invalid interaction targets are cleared; connector targets are supported as normal interactive elements.
- Open-URL interactions accept only `http`, `https`, and `mailto` during website playback.
- No new NuGet or JavaScript package was added.
