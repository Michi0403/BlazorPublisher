# PublisherStudio 1.0.58

## Explicit map mouse modes

- Map and Vector Map objects now have two mutually exclusive designer mouse modes instead of allowing the publication canvas and DevExtreme to compete for the same pointer gesture.
- **Move map object** disables native map panning and zooming, places a transparent designer shield over the map, and gives drag ownership to the publication object frame.
- **Pan / zoom map content** prevents object movement and gives pointer, touch, wheel, and double-click gestures only to the currently selected map widget.
- The active mode is visible and directly switchable in the canvas mouse indicator, the Component Tools ribbon, and the component context menu.
- Selecting another object, changing the primary selection, or creating a multi-selection exits map-content mode. An unselected map therefore cannot continue panning in the background.
- Provider Map and Vector Map now follow the same designer-mode contract. Vector Map panning, zooming, and its control bar are disabled in object mode and restored in content mode.
- Map center and zoom changes made in content mode are committed to the publication after a short debounce. Provider-map auto-adjust is disabled after a deliberate manual viewport change so the saved position is not immediately replaced.
- Content-pan pointer styling is scoped to the active selected object. Other text, spreadsheet, media, and Professional Component objects no longer inherit pointer suppression from a map editing session.
- Presentation, website, standalone HTML, print, PDF, image, and SVG output remain interactive or rendered through their established export paths; the new ownership rules apply only to the publication designer.

Application and installer version: `1.0.58`. Publication format remains `1.45`; picture format remains `1.2`.
