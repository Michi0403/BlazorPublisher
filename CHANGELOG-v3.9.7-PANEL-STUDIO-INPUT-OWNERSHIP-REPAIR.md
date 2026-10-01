# PublisherStudio 3.9.7 — Panel Studio and input-ownership repair

## Panel Studio authoring

- Repaired Panel Studio child rendering after the DevExpress maintenance-layout migration. Publication components now inherit their authored width/height through the Razor + `DxFormLayout` wrapper chain instead of depending on the old direct-child DOM shape.
- Added a wrapper-depth-safe renderer sizing rule for text, images, shapes, barcodes, spreadsheets, WordArt, DataVisuals, DevExtreme components, video/live sources and nested panels.
- Click insertion no longer drops every new component on the same canvas center. `PublicationElementLayoutService` now owns overlap-aware placement and chooses the nearest available position while keeping the requested bounds inside the panel canvas.
- Existing drag/drop and normalized Panel Studio geometry remain on the shared layout/interaction path; preset panel geometry and persisted publication formats are unchanged.

## Inspector and DevExpress controls

- Hardened the right Panel Studio inspector against narrow-column collapse: form grids adapt to the inspector's own available width, long object addresses/help text may wrap safely, and nested controls cannot force the inspector wider than its column.
- Replaced the small native behavior/script action buttons in the affected inspector with `DxButton` controls. Captions are allowed to wrap inside the button rather than escape their bounds.
- Kept the existing component list, authoring hitboxes and other intentional interaction surfaces intact rather than replacing working editor mechanics wholesale.

## Shared input ownership

- Extended the existing controller/mouse/keyboard arbitration into one shared native/editor ownership boundary instead of adding a Text Studio-only workaround.
- Focused or actively dragged text inputs, text areas, select/combobox editors, spin editors, sliders/range sliders, scroll thumbs, RichEdit surfaces and equivalent DevExpress/DevExtreme controls now temporarily own input.
- While such a control owns input, application/controller cursor actions and semantic canvas/Panel Studio gamepad actions yield. Controller button states are synchronized while suspended so a held button does not become a stale edge when the editor releases ownership.
- Canvas pointer routing no longer focuses the publication stage before a slider/editor has a chance to receive the pointer. Canvas keyboard routing uses the same ownership decision, protecting text caret navigation and slider arrow-key interaction.
- Controller support is not disabled; ownership returns to the canvas/studio automatically after editor focus/drag ends.

## Maintenance guards

- Extended the Panel Studio authoring-geometry guard to require wrapper-safe component sizing and shared overlap-aware insertion placement.
- Extended the Panel Studio interaction-lifecycle guard to require the common native/DevExpress input-ownership boundary for controller, canvas and Panel Studio routing, including the actual DevExpress Blazor host tags used by the maintained UI.

No `dotnet`, MSBuild, restore, publish, GitHub, or online repository access was used for this source handoff.
