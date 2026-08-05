# Animation and interaction

Animations belong to the publication model, not to one export format.

Each object can own entrance, emphasis, motion, media, and exit steps. Pages can own transitions, and objects can respond to clicks or presentation events.

## Timeline rules

Animation order is page-wide. This gives the timeline one deterministic sequence across text, pictures, shapes, media, panels, and data visuals.

## Presentation actions

Interactions can navigate pages, open safe URLs, show or hide targets, replay animations, and control media. An object can be hidden at presentation start while remaining visible and editable in the authoring workspace.

## Export mapping

The HTML runtime maps semantic animation records to browser animations. Print and static images show the authored visual state. Video export renders the timeline. Unsupported behavior should be reported as a capability or loss marker rather than silently disappearing.
