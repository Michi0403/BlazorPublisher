# PublisherStudio 4.3.5 — Picture Studio canvas and animated GIF repair

## Source and boundaries

Based exclusively on the user-supplied PublisherStudio **4.3.4** archive, compared with the supplied **4.2.2** working Picture Studio implementation. No GitHub access and no .NET build were used. Existing DevExpress, 1-Wire, publisher asset/encoding, PictureDocument, and state-service architectures are retained. PowerShell architecture policies and baseline enforcement are **not** relaxed.

## Picture Studio restored and extended

- Fixed the interactive canvas binding sequence: initialize and bind the browser canvas before any asynchronous image/SVG/GIF decode can reenter the Blazor rendering lifecycle. Rendering now requires successful initialization. The previous ordering could invoke `renderPictureStudio` without a registered editor, leaving the canvas at its browser-default 300×150 size (a small blank checkerboard at 65% zoom).
- Restored the proven 4.2.2 direct canvas render scheduler with its pointer/drawing pipeline, without removing the GIF frame filtering and export/import capabilities from 4.3.4. When a retained editor is given a replaced DOM canvas, it rebinds the gesture handlers. Picture Studio JS module imports have a refreshed version query to prevent loading cached broken modules.
- Retained asynchronous, chunked GIF frame decoding with per-frame timing, frame selection and layer-level raster editing. Frame N's raster content renders only in frame N. A failed frame import preserves the original still-image placeholder rather than deleting the open picture.
- Added **frame-local paint overlays** (flagged in the shared PictureLayer contract); drawing/erasing on one animated frame no longer paints the same overlay onto every other frame. The document's timing synchronization distinguishes authoritative frame layers from paint overlays and follows frame reorder/removal without treating overlays as extra GIF frames.
- Added an explicit **Convert visible layers to GIF frames** Output action. The operator chooses to turn a static multilayer picture into a timed animation; layer kinds, data and transforms remain editable and the change is undoable. Export/Apply Merged continues to compose the entire GIF and transfers the real `data:image/gif` result in bounded chunks.
- Disabled Apply and GIF Download while a pending GIF is still being decoded, preventing a user action from prematurely saving only the still placeholder.

## Architecture/build repairs from supplied Visual Studio diagnostics

- Restored the exact typed Razor logger `@inject ILogger<MediaStudioFooter> Logger` required by `RAZORLOG0001`.
- Added per-service structured `ILogger<T>` diagnostics and method-local `try/catch` boundaries to the three `MediaConversionOrganicHandlers` without changing their declared 1-Wire capabilities or approval semantics.
- Removed three shipped Python static-test files, which violate PublisherStudio's PowerShell-only maintenance inventory. Their bridge/catalog/approval assertions are retained as **PowerShell-only** regression checks in `tests/static/Assert-OrganicReleaseContracts.ps1`.
- Added `tests/js/picture-studio-canvas-regression.test.mjs`; retained and executed the existing `picture-studio-gif-frames.test.mjs`. No project architecture guards, exception baselines, or logging policy scripts were changed.
- Version increased **4.3.4 → 4.3.5** for both Web and InstallerConsole.

## Verification and limitations

- Chromium browser test (outside the shipped repository) passed: DOM canvas initialization; actual red/blue raster frame pixel switching; dimensions; brush `PictureStrokeCommitted` pointer callback; two-frame GIF encode/decode; and the complete `begin → append → finishPictureStudioGifApply → Begin/Append/CompletePictureExport` data URL delivery path.
- Node tests passed for 4- and 17-frame GIF fixtures, transparent GIF timing, WebCodecs fallback, corrupted input rejection, and updated canvas/layer ownership source contracts.
- **Not compiled** in .NET/DevExpress; the PowerShell gates require a Windows/PowerShell host and were not executed in this environment. Real Blazor popup sizing and end-to-end publication state still require verification in the installed application. Source-level/Chromium tests are not equivalent to a full application build.
