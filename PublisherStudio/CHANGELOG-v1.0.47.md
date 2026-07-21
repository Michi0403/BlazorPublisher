# PublisherStudio 1.0.47

## Document-first streaming

- Added a dedicated **Streaming** ribbon tab without introducing a second scene editor. Publication pages remain the program content.
- Added a large **Streaming Studio** modal for reusable encrypted provider profiles, publication outputs, recording, LAN delivery, devices, quality presets, timestamps, and streaming hotkeys.
- Added Twitch, YouTube, Kick, TikTok, custom RTMP/RTMPS, and custom SRT output profiles. Publications store profile IDs rather than provider secrets.
- Added dry-run sessions, provider test-mode flags, per-output scaling/bitrate/codec settings, runtime output toggles, program-page selection, and optional recording.
- Added clean-master, all-enabled-output, and selected-output recording variants with segmented crash-tolerant file writing, storage estimates, graceful encoder finalization, and optional MKV-to-MP4 remux after stop.

## Live sources

- Added normal publication elements for camera, screen, window, browser tab, capture device, microphone, system audio, application audio, network media, and Now Playing metadata.
- Live sources use the same page placement, resizing, layering, animation, connector, hotkey, and viewport workflows as existing objects.
- Added audio volume, mute, delay, device timestamp preference, crop/fit, brightness, contrast, saturation, hue, blur, and chroma-key controls.
- Chroma key includes similarity, edge smoothing, spill reduction, and residual opacity for deliberate translucent effects.
- Added browser audio mixing at 48 kHz for display-capture audio and active browser media sources. System/application audio first uses the browser window/system-audio picker and leaves native process-loopback as the explicit fallback.
- Added reusable machine-level device profiles. Browser cameras and microphones can be discovered, saved, and applied from the live-source inspector in later publications.

## Local Media Host

- Added the `PublisherStudio.MediaHost` project and release packaging.
- The Media Host supervises FFmpeg output, segmented recording, output toggles, HLS generation, LAN access tokens, Windows global hotkeys, and Now Playing ID3 metadata.
- Added automatic FFmpeg hardware-encoder discovery and initialization probes for NVENC, Quick Sync, AMF, and VideoToolbox with software fallback.
- Added exponential reconnect for unexpectedly terminated provider, recording, and HLS encoder pipelines.
- Added browser WebM ingest over a local WebSocket and fan-out to configured RTMP/RTMPS/SRT outputs.
- Added an explicit-IP LAN server with configurable output scaling/bitrate, a token-protected browser watch page, low-latency WebM playback, and VLC-compatible HLS URLs.
- Added session cleanup so active encoders, LAN listeners, and hotkeys are released when a session or host stops.

## Chat output context

- One authored Chat component can resolve its provider and channel from the active output context.
- Operator mode can switch between configured provider conversations.
- Broadcast mode omits the answer field and send button from the rendered Chat DOM.
- The current first implementation prepares the output-context contract and isolated Chat rendering; simultaneous provider-specific Chat composition still requires the multi-render compositor stage described in `docs/STREAMING.md`.

## Format and packaging

- Application, installer, Media Host, and web asset package version updated to `1.0.47`.
- Publication format updated to `1.44`.
