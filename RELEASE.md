# PublisherStudio 3.6.7

PublisherStudio 3.6.7 fixes designer object movement for embedded/composited content and removes unnecessary browser animation-frame polling. Selection is kept optimistic in the DOM during pointer-down and committed to Blazor only after a click or the final drag commit, preventing a server rerender from replacing an HTML/canvas object's DOM node between pointer-down and pointer-move. HTML embed and Panel surfaces use the transform-compatible editor zoom path so their independently composited iframe/canvas layers stay attached to the publication object while it moves.

Canvas and Panel Studio gamepad polling is now demand-driven: no permanent `requestAnimationFrame` loop runs merely because an editor surface exists. Polling starts only while a connected gamepad and an active relevant editor surface require it, and stops on hidden/disconnected/inactive state.

The 3.6.6 metadata-backed console identity and 3.6.5 macOS packaging subprocess repair remain intact.

See `CHANGELOG-v3.6.7-DESIGNER-DRAG-COMPOSITOR-STABILITY.md` and `VALIDATION-v3.6.7-source.md`.
