# PublisherStudio 4.2.3 — GIF editing/export and story underline recovery

Source-only release based on the complete PublisherStudio 4.2.2 repository archive.

## Corrected / implemented

- Normalize incorrectly inherited RichEdit underline decoration in the publication story HTML preview. Underlines are applied to actual non-whitespace glyph runs, in the glyph color rather than the color of a differently styled outer span; separators and trailing spaces are not underlined. RichEdit's stored document itself is left intact.
- Import an inserted GIF into Picture Studio as independently selectable, editable PNG-backed raster layers, one per decoded animation frame; retain per-frame durations and a shared animation timeline. Timed frame controls let the user see and select each frame.
- Preserve GIF motion when **Apply layered** or **Apply merged** returns an image to the publication; neither path silently converts an animated document to PNG. Add **Download animated GIF** in Picture Studio.
- Add **Render selected range to animated GIF** in Video Studio, sampling its current effect-canvas renderer. GIF output is visual-only (no audio track).
- Use a shared dependency-free GIF89a RGB332 encoder and the browser's ImageDecoder for import. GIF frame transfers to Blazor use bounded chunks. Encode/decode errors are surfaced instead of silently falling back to a still image.
- Revise relevant JavaScript module cache keys to 4.2.3 and increment the Web and InstallerConsole project versions to 4.2.3.

## Limitations and verification

- GIF uses an indexed 256-color palette and binary transparency, the GIF format's constraints; exact full-color fidelity is not promised.
- Editing animated GIFs requires an up-to-date Chromium-based browser with WebCodecs `ImageDecoder`; otherwise the editor reports the limitation. Import/export include frame/pixel caps to protect the app from memory exhaustion.
- Tested GIF89a encoding with Node.js plus independent ImageMagick/Pillow decoding (three frames, differing color patterns, and original 40/140/240 ms delays). Browser JS syntax was parsed via `node --check` for the four changed modules.
- The source was **not compiled in .NET** and the .NET/DevExpress UI workflows have **not been run** in this environment. Runtime regression status remains to be established on the user's Windows development installation.
