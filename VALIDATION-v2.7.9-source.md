# PublisherStudio 2.7.9 source validation

This package was validated as source only. No `dotnet build`, restore, publish, pack, application launch, GitHub access, or online repository access was used to produce it.

## Supplied-media diagnosis

- `Recorded Video (8).webm` inspected with local `ffprobe`:
  - VP8 video.
  - 3828 × 1962 pixels.
  - YUV 4:2:0 pixel format.
  - 3,093,642 bytes.
- The recording emits variable-rate packets, including approximately one video packet per second through static intervals, while motion-heavy intervals are commonly around 2–3 Mbit/s. Duplicate/static-frame suppression is normal for browser recording, but the motion bitrate is too low for robust near-4K UI/text capture.
- The WebM data URI extracted from `Untitled Publication (53).html` is exactly 3,093,642 bytes and is byte-for-byte identical to the supplied WebM.
- Both copies have SHA-256 `109d6aa19b63e4f0b6d398045595ee1bebd6dd21af93a3c3ed4d0bd5ae6c6ac3`.
- Therefore the standalone HTML export does not recompress the supplied recording.

## Source checks

- `node --check` passed for `src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js`.
- `appsettings.json` and all six maintained localization JSON catalogs parse successfully.
- The maintained JavaScript SHA-256 inventory was refreshed for the changed Media Studio interop file.
- `build/audit_release_2_7_9.py` passes and verifies:
  - PublisherStudio 2.7.9 version metadata.
  - capture-size, frame-rate, codec, and bitrate controls.
  - 32,000-kbit/s browser-recording video default and 192-kbit/s audio default.
  - VP9-first automatic selection with VP8 fallback.
  - explicit MediaRecorder bitrate options.
  - display-capture constraints and post-capture constraint application.
  - localization coverage.
  - JavaScript diagnostics hash consistency.
  - the unchanged explicit InteractiveServer boundary count.
- Retained release audits through 2.7.8 pass after the active source-version assertions and maintained JavaScript hash inventory were rolled forward.
- Application architecture audit passes.
- Service resilience audit passes.
- Strict async-continuation audit passes.
- PublisherStudio's explicit `@rendermode InteractiveServer` file set is identical to 2.7.8.

## Runtime verification still required

Because this package was intentionally not built or run in .NET here, the authoritative verification remains your normal local build and browser test. In particular, verify the browser's returned start banner for actual capture resolution/frame rate/bitrate, because WebRTC/display-capture implementations may legally keep native surface settings when an ideal constraint cannot be satisfied.
