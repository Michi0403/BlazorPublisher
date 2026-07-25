# PublisherStudio v1.0.82

## OpenRaster compiler repair

- Restored the missing `ReadInt`, `ReadDouble`, `MapBlendMode`, `DecodeSvg`, `DecompressSvg`, and `ReadBigEndianInt` helpers in `OpenRasterImportService`.
- Integer and floating-point OpenRaster attributes now use invariant, finite-value parsing with documented fallbacks.
- OpenRaster composite operations now map the supported normal, multiply, screen, overlay, darken, and lighten modes to the canonical Picture Studio blend modes.
- SVG layers use strict BOM-aware text decoding; SVGZ layers use the BCL GZip stream and continue through the existing SVG sanitizer.
- PNG dimensions are read safely from the big-endian IHDR fields.
- Deconstructed viewport and image-size tuples remove the two reported IDE0042 suggestions.

## Media component frontend parity

- Media Converter Studio now opens completed image, audio, and video output in Picture Studio, Audio Studio, or Video Studio according to the result MIME type.
- Converter source editing is likewise media-aware instead of being limited to Video Studio.
- Audio Studio can now send its selected trimmed clip to Media Converter Studio with an audio-specific WebM/Opus preset and trim options.
- Video Studio retains its selected-range handoff and video-specific suggested conversion options.
- Unknown browser file MIME values are resolved from the file extension before choosing the matching preset or Studio.

## Context actions, labels, and tooltips

- Conversion-job cards now wire their existing job-specific context-menu handler, so right-click selects the intended job before showing actions.
- Completed-job double-click behavior now opens all supported media types in their matching Studio.
- Added explicit close, browse, convert, download, insert, cancel, remove, and Studio-open tooltips to native Media Converter controls.
- Updated visible job instructions to describe selection, right-click actions, and cross-Studio double-click behavior.
- Source/result Ribbon and context-menu labels now identify the actual destination Studio.

## Compatibility

- Application, installer, npm, lock-file, streaming runtime, browser runtime, CSS release marker, and version contract tests advanced to `1.0.82`.
- Publication format remains `1.55`; no document migration is required.
- Added `type: module` to the web asset package metadata so the ES-module regression imports no longer produce Node module-type warnings.
- NuGet and npm dependency names and versions are unchanged.
