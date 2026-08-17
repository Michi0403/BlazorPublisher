# PublisherStudio 2.8.0 — adaptive media quality and capture-pressure hardening

## Changed

- Replaced machine-specific recording assumptions with a configurable adaptive media-quality policy shared by browser recording, publication outputs, LAN delivery, and media-host session wiring.
- Smart recording now waits for the browser's actual selected track geometry/frame rate and may use `MediaCapabilities.encodingInfo` plus `MediaStreamTrack.getSettings()` to rank supported recording codecs and calculate video/audio bitrate targets. Capability probes use codec-specific video content types and the real audio-track channel/sample-rate information instead of treating a combined recorder MIME string as a video capability description.
- Added independent, user-configurable switches for adaptive video, adaptive audio, provider-quality knowledge, browser capability probing, native-resolution preservation, FPS fallback, and last-resort resolution fallback.
- Added Efficiency / Balanced / Quality profiles. They are policy multipliers, not fixed monitor presets, and every automatic value remains overridable through manual/custom modes.
- Provider output recommendations, LAN audio/video recommendations, encoder validation fallbacks, and browser recording now use runtime policy/shared quality advice rather than one developer machine's display characteristics.
- Provider-specific quality tiers remain configuration data. They can be disabled per publication; an empty provider-profile collection no longer invalidates runtime policy.
- Browser recording poster analysis no longer needs to allocate a source-sized 4K canvas by default. Poster pixel budget and post-capture analysis delay are configurable, while the retained WebM/recording bytes remain untouched.
- Browser capture sets content hints (`detail` for screen capture, `motion` for cameras), preserves native source resolution by default, and only lowers FPS/resolution when the configured smoothness policy permits it.
- Recording diagnostics report the actual accepted width, height, FPS, MIME/codec and recorder bitrate after the browser opens the selected source.
- Added localized Media Studio adaptive-quality labels for en-US, de-DE, fr-FR, es-ES, ja-JP and uk-UA.

## Preserved

- Existing Media Studio editing, retained-recording recovery, download/save/insert/apply, trim/crop, regions, filters, layer ordering, sequence/timeline, project import/export and playback workflows were not removed.
- Existing streaming provider, recording, LAN, browser/WebRTC, HLS, RTSP and encoder features remain available; adaptive behavior is an optional preselection layer with manual overrides.
- InteractiveServer render boundaries are unchanged from 2.7.9.

## Source-only note

This repository was edited and statically validated without invoking dotnet restore/build/publish/pack and without GitHub access.
