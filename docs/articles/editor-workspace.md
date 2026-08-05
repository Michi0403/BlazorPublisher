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
