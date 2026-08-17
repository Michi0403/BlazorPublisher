# PublisherStudio 2.7.9 — browser recording quality

## Fixed

- Browser screen and camera recording no longer relies on the browser's very low implicit MediaRecorder bitrate.
- Media Studio now passes explicit video and audio bitrate targets to `MediaRecorder` while retaining the existing recording lifecycle, retained-Blob recovery, reconnect handling, preview watchdog, save, insert, and apply workflows.
- Automatic WebM selection now prefers VP9 when the browser can both record and play it, with VP8 and generic WebM retained as fallbacks. Users can explicitly prefer VP8 or VP9.
- Browser capture now requests the selected capture dimensions and frame rate with ideal constraints and then applies those constraints to the resulting video track when supported. Browsers remain free to preserve the selected surface's native dimensions rather than failing capture.
- Media Studio reports the actual capture dimensions, frame rate, MIME type, and recorder bitrate returned by the browser when a recording starts.

## Added

- Recording capture-quality controls in Media Studio:
  - Source/native capture size.
  - Streaming master size (policy default 3840 × 2160).
  - Streaming output size.
  - Custom width and height inside the existing runtime-policy bounds.
  - Capture frame rate.
  - Automatic codec preference (VP9 first), explicit VP9, or explicit VP8.
  - Video bitrate and audio bitrate.
- Dedicated browser-recording policy defaults independent from the existing stream-output bitrate:
  - `BrowserRecordingVideoBitrateKbps`: 32000.
  - `BrowserRecordingAudioBitrateKbps`: 192.
  - `BrowserRecordingCodecPreference`: `auto`.
- Localized labels and explanations for all six maintained PublisherStudio UI cultures.

## Evidence behind the repair

The supplied screen recording is already captured at 3828 × 1962, so insufficient capture dimensions are not the primary fault. The WebM is only 3,093,642 bytes for roughly 15.7 seconds and uses VP8; motion-heavy seconds are generally only around 2–3 Mbit/s. That is far below a sensible target for crisp near-4K text/UI capture.

The WebM embedded in the supplied standalone HTML presentation is byte-for-byte identical to the supplied recording (SHA-256 `109d6aa19b63e4f0b6d398045595ee1bebd6dd21af93a3c3ed4d0bd5ae6c6ac3`). The HTML exporter therefore did not transcode or progressively reduce the recording quality. Time-dependent softness visible in that playback is already present in the recorded WebM rather than being introduced by HTML export.

## Preserved behavior

- No Media Studio feature was removed.
- Existing segment editing, project import, layer/order behavior, crop/region handling, retained recording recovery, recording download/save/insert/apply behavior, preview reconnection, and streaming integration remain in place.
- Existing `InteractiveServer` component boundaries are unchanged.
- LocalGPT and the LocalGPT 1-Wire protocol are unchanged by this PublisherStudio release.
