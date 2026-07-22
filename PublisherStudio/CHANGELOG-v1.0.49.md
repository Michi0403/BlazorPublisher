# PublisherStudio 1.0.49

## PublishingSuite interface alignment

- Reworked Streaming Studio into the same document-oriented SubApp workflow used by the other PublisherStudio studios.
- Replaced the flat tab-and-form presentation with a real DevExpress ribbon, contextual command groups, a reusable profile navigation pane, structured settings cards, status summaries, and the standard SubApp title/footer treatment.
- Kept provider profiles, publication outputs, recording, Company/LAN delivery, devices, Media Host options, and hotkeys in their established locations and data flow.
- Added contextual ribbon commands for creating, saving, duplicating, selecting, recommending, enabling, and removing the current streaming item.
- Added optional workflow guidance for every Streaming Studio section without blocking the normal compact editing path.
- Added responsive layouts so dense output, recording, LAN, device, and hotkey settings remain usable in smaller browser windows.

## Direct object workflows

- Canvas double-click activation now runs in capture phase for every publication object, including nested DevExtreme controls, media, SVG, and other content that previously consumed the browser event.
- Right-click context menus now use the same capture-safe route, so embedded components can no longer suppress the PublisherStudio object menu.
- Existing object-specific actions remain intact: Story Editor, Picture Studio, Spreadsheet Studio, Data Visual Studio, Component Studio, Media Studio, Barcode Studio, shape/connector commands, and normal selection/transform behavior.
- Streaming Studio provider profiles, publication outputs, device profiles, and hotkeys now also expose right-click commands.

## Explanatory tooltips

- Added one application-wide tooltip runtime for native controls and dynamically rendered DevExpress controls.
- Explicit `data-help` descriptions explain workflow-sensitive controls; a shared command catalogue covers common ribbon, dialog, context-menu, media, data, publication, and streaming actions.
- Unannotated buttons, fields, selectors, checkboxes, links, tabs, and menu items receive a simple generated explanation instead of remaining silent.
- Tooltips work on pointer hover and keyboard focus, reposition inside the viewport, close on interaction or Escape, and respect reduced-motion preferences.
- Publication objects describe their double-click, right-click, drag, resize, crop, and studio-opening behavior directly on the canvas.

## Compatibility and validation

- Existing publication and Media Host models, provider encryption, output composition, recording, LAN, native-device, hotkey, and save/apply workflows are unchanged.
- No publication-format migration was required; publication format remains `1.45` and picture format remains `1.2`.
- Added an interface contract test covering the shared ribbon/context-menu workflow, capture-safe canvas activation, and global tooltip runtime.
- Updated the streaming runtime contract test for the corrected C# raw-string brace form.
- Application, installer, Media Host, and web package version updated to `1.0.49`.
