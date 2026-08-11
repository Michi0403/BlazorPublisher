# Editor workspace

The editor keeps page structure, object order, and reusable tools in one view.

## Main areas

- **Title bar** — publication name and save state.
- **Ribbon** — create, arrange, animate, stream, export, and open help.
- **Pages pane** — page selection and ordering.
- **Mainframe** — the active publication page.
- **Inspector** — properties for the selected page or object.
- **Timeline** — animation and media timing when enabled.

## Select and arrange objects

Click an object to select it. Use Ctrl or Shift for multi-selection. The Arrange commands move objects through the page layer stack, align them, or group them.

Snapping can use the grid, guides, page edges, and other objects. Rulers and guides are workspace aids; they are not exported as publication content.

## Interaction ownership

A gesture has one owner. The Mainframe owns ordinary page gestures. Picture, media, panel, and spreadsheet studios own their local editing gestures while open. Temporary handles, masks, and playheads never become publication layers.

## Safe cancellation

Closing or cancelling a studio leaves canonical page placement and layer order untouched unless you explicitly applied a result. This separation is one of the reasons the editor can support many object types without each tool inventing its own page model.

## Panel Studio geometry and reusable modules

Panel Studio owns a local canvas that is separate from the object's Mainframe placement. Moving or resizing a component inside Panel Studio changes that panel-local geometry; it must not rewrite the outer Mainframe X/Y/width/height.

When a standalone HTML/interactive object still fills the complete Panel Studio canvas, PublisherStudio can keep it as the lightweight standalone HTML element and apply content/configuration changes directly. As soon as that object is moved, resized, or rotated inside Panel Studio, applying the edit promotes the Mainframe object to a `PanelElement`. The original Mainframe placement stays unchanged while the authored local geometry is retained inside the panel graph.

Pointer and resize operations are queued through browser interop. **Save/update module**, **Save configured module**, and the main Panel Studio save now wait for all queued layout commits before cloning the module or panel. This prevents a fast save immediately after a drag/resize from capturing the pre-drag bounds.
