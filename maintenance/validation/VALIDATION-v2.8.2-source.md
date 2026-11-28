# PublisherStudio 2.8.2 source validation

This release was prepared under a source-only validation constraint. No `dotnet`, MSBuild, restore, build, publish or pack command was executed, and no GitHub/network repository source was used.

## User-supplied reproduction evidence reviewed

- The supplied PublisherStudio HTML snapshot retains the selected publication image while Picture Studio is open, while the separate JavaScript selection visual frame is already marked hidden. That pointed to background designer input/lifecycle leakage rather than a need to redesign z-index stacking.
- The supplied HTML also confirms widespread native `input type="range"` controls in the current UI, so the slider repair was made at the shared browser-interaction layer instead of patching one slider.
- The supplied publication JSON was retained as a compatibility fixture while reviewing the image/video state and no publication format migration was introduced for this repair.

## Static/source validation performed

- `node --check` passed for:
  - `wwwroot/js/publisherInterop.js`
  - `wwwroot/js/mediaStudioInterop.js`
  - `wwwroot/js/videoEffectRuntime.js`
- `build/audit_release_2_8_2.py` passed 167 targeted checks covering interaction suspension, range-event coalescing/final flush, configurable converter suggestions, preset guidance, responsive converter layout, rendered Video Studio export, effect-runtime reuse, adaptive recorder reuse, layer/effect retention, localization, browser cache revisions, JavaScript hashes, render boundaries and 1-Wire version.
- Retained `build/audit_release_2_8_1.py` passed 62 checks.
- Application architecture audit passed.
- Service-resilience audit passed for 1,292 service methods plus four reviewed iterator/yield methods.
- Async-continuation audit passed for 75 source files.
- Panel Studio persistence source audit passed.
- PublisherStudio documentation/1-Wire contract audit passed.
- XML documentation coverage/quality passed for 5,525 direct C# declarations across 189 maintained source files.
- `appsettings.json` and all maintained localization JSON files passed parsing with duplicate-key detection.
- Project/build XML files parsed successfully.
- The maintained JavaScript diagnostics SHA-256 inventory was refreshed for the modified browser files.

## Runtime qualification

The new rendered-video path requires browser support for `HTMLCanvasElement.captureStream` and `MediaRecorder`; audio baking additionally uses Web Audio when available. The implementation reports a visible export failure rather than silently substituting an unrelated server-side render. Because this package was not compiled or executed in a browser here, the user's local build and browser test remain the authoritative runtime validation.
