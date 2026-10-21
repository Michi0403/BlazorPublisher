# PublisherStudio 4.3.0 — Picture Studio and independent GIF-frame layers

Full source release, continuing forward from 4.2.9. No rollback of Picture Studio or removal of its existing layer/edit/effects features.

## Repairs

- The native Picture Studio document, its raster/vector/paint/text/render layers, and the original shared compositor remain authoritative. GIF support is an optional input/export capability, not a replacement editor.
- GIF import opens the original image as a raster first and replaces **only that raster layer** with one independently editable PNG-backed raster layer per decoded GIF frame. If an asynchronous decode fails, the source raster and other layers remain. Opening a GIF no longer uses the start-new-document mutation on completion; import callbacks from prior sessions are rejected.
- Imports preserve raster-layer position, dimensions, rotation, opacity, blend, clipping, visibility, lock state, flips, fit mode, tint, colorize settings, and shared image effects. Non-GIF layers are untouched.
- GIF frame decoding uses browser WebCodecs when working; a bundled dependency-free GIF87a/GIF89a fallback now supports local/global palettes, transparency, disposal, interlacing, frame counts and delays when WebCodecs is unavailable. Frame counts are verified before streaming results, with per-frame transfer and progress instead of buffering all decoded fallback frames in memory.
- The Layers sidebar shows each frame's current sequence index and duration; frame navigation selects the matching editable raster layer, including after opening a saved layered GIF. Moving, deleting, pasting or duplicating a timed layer reindexes the animation timeline while retaining each frame's duration. Clipboard cloning retains frame identity and timings; undo/redo restoration selects a visible frame.
- Layered SVG documents containing a saved animation preserve frame timing on import. A second independent timeline is rejected without overwriting an existing one; true multi-track animation is not represented as supported by the current BusinessObjects.
- Browser rendering retains 4.2.9's cancellation/coherent-frame canvas commit and active-frame-only JS interop projection. No change was made to general raster/vector/effects drawing pipelines.

## Limits / validation

- One authoritative timeline per picture is an existing BusinessObjects limit. Every imported GIF frame is an individual editable raster layer; it is not one composite raster layer.
- Browser failures are not concealed or misrepresented as successful frame import. The original raster remains available when animated conversion cannot finish.
- .NET SDK, Windows/Edge, and PowerShell are unavailable in this environment, so full compiler/build/interactive Windows results are **unverified**. Run the unmodified native build gates before deployment.
- The optional self-contained Node.js decoder fixture test does not add a mandatory Node/Python build dependency.
