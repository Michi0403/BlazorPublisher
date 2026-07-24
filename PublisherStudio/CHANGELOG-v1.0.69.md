# PublisherStudio v1.0.69

## Local spatial-selection workflow

- Video Studio frame-region mode now places a dedicated interaction layer above the native video player.
- The layer follows the actual contained source-frame rectangle instead of the letterboxed preview box.
- The preview is darkened outside the active region and uses a browser-local crosshair cursor.
- Native media controls are blocked only while frame-region mode owns the gesture.
- Apply, undo-point and exit actions are available directly on the selection surface; ribbon, property-panel, context-menu and keyboard actions remain available.
- The full source remains visible while editing. The committed clip-path is restored after apply or cancel.

## Picture Studio selection visibility

- Picture selection modes now use a local pointer-transparent guide and crosshair.
- The canvas is shaded when a spatial-selection mode is active.
- Rectangle, ellipse, freehand, magnetic and polygon selections keep the selected area visible while shading the outside.
- Selection boundaries and vertex handles are rendered after document layers without becoming Picture Studio layers or PublisherStudio page objects.

## Recording finalization repair

- Fixed `Math.Clamp` receiving a minimum greater than the maximum when a just-finished recording was shorter than the normal trim step or briefly reported a zero duration.
- Trim ranges are normalized through one finite-duration helper before committing.
- Range-selector minimum span and scale end are derived from the actual clip duration.
- Localized/numeric range values are parsed defensively without allowing the UI callback to interrupt Stop Recording.

## Frontend architecture contract

- Added ADR-010 and expanded the repository AI/contributor rules for local interaction overlays, one-owner pointer gestures, source-frame coordinate mapping, local stacking contexts, observer/listener cleanup and Mainframe Z-order preservation.
- High-frequency cursor feedback stays in JavaScript/CSS; Blazor Server receives committed points and commands.
- Audio remains intentionally one-dimensional.

Application and installer version is `1.0.69`. Publication format remains `1.49`, Picture Studio format remains `1.4`, and no NuGet, npm, native binary or external process dependency was added.
