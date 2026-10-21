# PublisherStudio 4.2.9 source validation and Windows acceptance

## Source-level checks

- Full 4.2.8 base preserved; only version references, Picture Studio component, PictureDocumentService, PictureEditorStateService, Picture Studio JS, maintained diagnostics manifest and release documentation changed.
- Node JS parser accepted the changed `pictureStudioInterop.js` and retained `animatedGifInterop.js`.
- Async rendering race test committed only the latest document after an older decode completed late, with target native pixel dimensions intact.
- Node GIF89a encoder produced three distinct animated frames; independent Pillow decoder validated dimensions and frame delays 40/130/250 milliseconds.
- JavaScript diagnostics SHA-256 inventory refreshed; PowerShell audit implementation not modified or bypassed.
- GIF import retains unrelated animation layers (no blanket `RemoveAll(AnimationFrameIndex)`), stages changes, and preserves placeholder geometry/z-order.
- Browser rendering and GIF Apply/Download use per-frame projection instead of serializing every PNG payload in one message.
- SVG vector import avoids unnecessary raster Image decoding, retains complex sanitized vector content as one editable SVG layer when needed, with explicit compatibility warning.

## Required on-device acceptance before deployment

Build the complete Debug solution with normal architecture guards and mandatory documentation generation, then inspect Picture Studio in the running browser:

1. Open an ordinary PNG and JPEG and WebP image: canvas pixels, raster colorization, drawing, rotation, selection and Apply layered/merged still work.
2. Add an SVG through both normal image import and layered import: shapes should remain available as editable vector layers where decomposable, otherwise one complete sanitized vector layer must appear; exported result must not be blank or cropped.
3. Open a 17-frame GIF: the original still must display immediately, frame decoding should promote it to 17 independently editable timed layers without blanking the canvas. Select frames 1, 15 and 17; confirm each frame's content and hit testing match the visible frame.
4. Add a GIF to an existing layered ordinary picture: unrelated layers, positions, effects and stacking order must remain intact. Replacing one image should not replace other layers. Attempting to add a second timeline must retain the existing timeline and inserted still, and show the supported limitation.
5. Change paint, shape, vector, global raster and effects while a frame is selected, then run GIF Download, Apply layered and Apply merged. Check the exported GIF has all expected frames and timing; the retained layer document should reopen correctly.
6. Trigger edits/selection while a large raster/SVG is still loading: no older asynchronous render should clear or overwrite the newer canvas.
7. Verify notification/error logging, correct image opacity, production picture canvas dimensions, and Mainframe publication selection/cropping after Picture Studio closes.

## Environment constraint

This container has Node.js and independent image decoding, but not `dotnet`, Windows PowerShell, licensed DevExpress or a functional interactive Windows Chromium host. Consequently there is no confirmed Windows compiler result or end-to-end DevExpress Picture Studio runtime result here. All architecture guards remain mandatory and unmodified.
