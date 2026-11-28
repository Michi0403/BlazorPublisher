# PublisherStudio 4.3.7 — Picture Studio canvas / GIF frame visibility recovery

## Source baseline

Based on the complete PublisherStudio 4.3.6 source ZIP, with PublisherStudio 4.2.2 separately inspected as the known-working drawing editor. No component, capability, policy, or GIF frame model was removed. No GitHub or .NET build was used; the existing PowerShell architecture gates and `Directory.Build.targets` were left untouched.

## Root-cause analysis

The 4.3.6 Picture Studio C# component used a one-time `_initialized` state to represent the browser `<canvas>`. The DevExpress popup owns a separate portal lifecycle and can replace/attach its DOM subtree without recreating the Blazor component. Thus `_initialized` could be true while the live `<canvas>` was not bound: HTML then displayed the browser-default 300×150 backing canvas and 200×100 CSS checkerboard seen in the reported screenshots. The prior regression test only asserted a code string and could not detect this actual DOM replacement.

Also, image/SVG/GIF raster decoding was asynchronous and rendered directly onto the visible canvas; a newer frame/document render could be overwritten or cleared by an older asynchronous invocation.

## Corrections

- Reconfirm the live canvas binding on **every** Picture Editor `OnAfterRenderAsync`, not just the first; `initializePictureStudio` now has an actual Boolean bound/unbound result.
- Keep a narrowly scoped browser `MutationObserver` attached to the editor lifetime, rebind when DevExpress replaces the popup `<canvas>`, and redraw the existing document. Disconnect it on editor disposal. If the canvas appears **after** an attempted initialization, a new typed Blazor callback (`PictureCanvasAttached`) requests the authoritative document instead of silently leaving a checkerboard.
- Rebind the image drop host only when its DOM identity changes; normal server renders no longer repeatedly tear down active drag/drop listeners.
- Render raster/vector layers and individual GIF frames into a **private staging canvas**; commit pixels to the visible canvas only if the requested frame, document generation, canvas element, and active editor still match. Asynchronous decoding no longer clears a previously good image while it loads.
- Invalidate stale render tokens immediately when a new document/frame reaches the browser instead of waiting for the next animation frame.
- Restore explicit errors when rendering is requested without an initialized browser editor, rather than returning silently.
- Preserve all 4.3.6 GIF frame selection/editing, original working canvas pointer events, PNG/JPEG, and animated Apply Merged/Download logic; no drawing tools or layering functions were removed.
- Update the Web and InstallerConsole assembly versions to 4.3.7, both picture module call sites in the publication editor, the Picture Editor module version, and the maintained JS diagnostics SHA-256 inventory.

## Validation and limitations

- `node --check` on Picture Studio and animated GIF modules: passed.
- Existing `tests/js/picture-studio-gif-frames.test.mjs`: passed.
- Strengthened `tests/js/picture-studio-canvas-regression.test.mjs`: passed. Its assertions now describe the actual portal and async-frame lifecycle rather than the previous render scheduler string.
- Real headless Chromium browser test **outside the production repository**: verified exact pixel colors and intrinsic dimensions for a PNG, DOM canvas replacement recovery, two different GIF frame selections, brush `PictureStrokeCommitted` through the original pointer path, a late-mounted popup canvas and `PictureCanvasAttached` callback, with zero browser page errors. A separate real Chromium test encoded an animated **Apply Merged** result and decoded it back into two distinct frames with 100/200 ms delays (expected GIF palette quantization). The test loads the production JS module and its codec; it does **not** boot Blazor or DevExpress.
- 17 maintained JS diagnostics SHA-256 hashes verified against the final source snapshot; ZIP integrity checked.
- **Not executed:** PublisherStudio running in .NET / DevExpress, full PowerShell architecture gates, Visual Studio compilation, real large GIF end-to-end Apply to publication. This is source-plus-browser validation, not a claim of a passing application build.
