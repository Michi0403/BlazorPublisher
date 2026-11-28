# PublisherStudio 4.3.0 source-only validation

- Base: full 4.2.9 source repository. No rollback or deletion of previous features, compiler guards, source tests, or core source files.
- JavaScript: `node --check` on maintained browser modules; optional `node tests/js/picture-studio-gif-frames.test.mjs` validates 4 transparent timed GIF frames, 17 distinct composited GIF frames, and rejection of truncated GIF streams. GIF fixture pixels and delays were compared against independent Pillow decoding during development; Pillow is not a runtime or build dependency.
- Changed-document source review: visible frame selection in StartFromDocument/Restore and SetActiveAnimationFrame; timeline synchronization for duplicate/paste/delete/reorder; clone preservation of frame duration metadata; shared raster effects preserved when replacing a temporary raster with independent GIF frames; new-session stale import rejection.
- JavaScript diagnostics inventory regenerated using the original newline-normalized SHA256 contract.
- No .NET SDK, PowerShell, DevExpress runtime/browser interactive UI validation available in this container. No claim of a successful Windows Debug build or working End-to-End Picture Studio test.

## Windows manual acceptance sequence

1. Open PNG/JPEG/WebP with transparency; add text, paint, SVG, vector, and render layers; reposition, hide/show, blend, adjust effects, duplicate, undo/redo, save and reopen; confirm the image never clears on redraw.
2. Open the original multi-frame GIF. The Layers list must grow to the GIF's actual frame count after progress reaches completion; selecting each GIF frame in the list and using Previous/Next must select the corresponding individually editable frame.
3. Rotate/crop/flip/recolor a selected frame. Other frames retain independent modifications; timeline delay values and frame order stay associated with their frames after moving, duplicating or deleting frames.
4. Apply layered/merged and Download animated GIF; reopen and inspect playback/frame count; ensure non-GIF layers remain unchanged.
5. Import a complex standalone SVG, including one with unsupported atomic vector decomposition: fallback preserves it as a single SVG layer, not a blank document.
6. Test two separate Studio sessions and verify a slow prior GIF import cannot write into the new document. Confirm browser console and file logger diagnostics for failures.
