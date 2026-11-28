# PublisherStudio 4.2.4 — architecture compliance and GIF recovery follow-up

Full source release based on the previously supplied PublisherStudio 4.2.3 source archive. No architecture assertion was disabled, weakened, or removed.

## Build-policy repairs

- `L10N0003`: registered new Video Studio GIF export and Picture Studio GIF import/frame UI texts in `en-US.json`, `de-DE.json`, and every other maintained culture. German has actual translations. Original localization keys and values were retained.
- `PSDX0002`: replaced the two newly introduced native GIF frame navigation `<button>` elements with `DxButton` controls, preserving the native interactive-element baseline and render/circuit semantics.
- Service resilience: `PictureEditorStateService.ImportGifFrames` and `SetActiveAnimationFrame` use method-local try/catch with trace diagnostics, consistent with the service's existing architecture.
- JavaScript diagnostics: the new `animatedGifInterop.js` has function-level guarded error reporting and is registered in the canonical SHA-256 diagnostics manifest. Existing GIF interop and story underline routines have additional error logging; all maintained JS manifest hashes were regenerated from their actual sources.

## Functional and reliability refinement

- GIF imports stage and validate a detached canonical picture document before updating the live editor, avoiding partial live-document corruption when frame import fails.
- GIF frame navigation uses the current active frame while preserving frame timing and separately editable image layers; GIF Apply/Download and Video Studio GIF output remain enabled.
- The story HTML preview underline correction remains at the rendering boundary; it now guards style detection against elements with no inline style and reports failures through the existing JavaScript diagnostics pathway.
- PublisherStudio Web and InstallerConsole versions were incremented from 4.2.3 to 4.2.4; changed module cache keys were updated accordingly.

## Verification/limitations

- **Not built in .NET**: .NET/Windows/DevExpress were unavailable here. This release has not been proven compilation-clean or interactive-regression-free in the actual Windows application.
- Independently ran JSON parsing, cross-culture key/value-integrity checks, literal `LT` coverage, German translation/baseline checks, native Razor tag counts, JavaScript diagnostics manifest/hash checks, `node --check` for all maintained JavaScript modules, and a multi-frame GIF encode/decode test using Node/Pillow (four frames with timing and transparency).
- The native repository PowerShell validation scripts could not be executed in this environment; their checks were mirrored at source level where feasible. **Run the unmodified repository build/PowerShell guards and .NET build locally before deploying.**
- Browser `ImageDecoder` remains required for editing multi-frame GIFs in Picture Studio. GIF palette/color and transparency restrictions and performance limits remain inherent in the new browser codec path. Animation features still need real-browser UI testing.
