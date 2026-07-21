# PublisherStudio 1.0.48

## Multi-output program compositor

- Completed the shared-base streaming compositor. PublisherStudio captures the publication once without authored Chat DOM, then produces an independently scaled canvas for each required output.
- The same authored Chat component is resolved against each output's provider/channel context and composited only into that output.
- Twitch viewers receive the Twitch Chat layer, YouTube viewers receive the YouTube Chat layer, and the rest of the page remains synchronized.
- Broadcast canvases never create the Chat composer, message field, or send button. Those controls remain operator-only.
- Output variants are created only when the provider is enabled or the variant was explicitly selected for recording.

## Provider Chat hub

- Added encrypted, reusable Chat OAuth credentials separately from stream keys.
- Added a Media Host Chat hub with isolated history and WebSocket subscriptions per output.
- Added native Twitch IRC receive/send support with tags, badges, timestamps, reconnect, and channel isolation.
- Added YouTube Live Chat receive/send support with OAuth, paging, polling intervals, deduplication, and reconnect.
- Added an operator bridge that switches between configured provider chats without merging their messages.
- Kick, TikTok, and custom providers remain valid streaming outputs; their Chat adapters are explicit extension points until those providers expose/permit a compatible authenticated API.

## Native capture and audio

- Added FFmpeg-backed native camera/capture-device discovery for Windows DirectShow, macOS AVFoundation, and Linux V4L2.
- Added native capture sessions streamed back to the browser as bounded WebM feeds.
- Added Windows process-tree audio discovery and process-isolated WASAPI loopback capture for application sources such as Discord or a game.
- Browser window/system-audio capture remains the cross-platform fallback.
- Device timestamp preference is carried through browser capture, native capture, audio mixing, and FFmpeg input handling.

## Company/LAN delivery

- Added low-latency browser playback through WebRTC signaling, with automatic WebM/MediaSource fallback.
- Added token-protected HLS output for browsers and VLC.
- Added an RTSP control server with interleaved RTP-over-TCP delivery suitable for VLC and other RTSP clients.
- LAN listeners remain disabled by default, bind only to the explicitly selected address, enforce viewer limits, and can require an access token.

## Reliability and recording

- Provider, recording, HLS, and RTSP encoder pipelines remain independent so one failed destination does not stop the others.
- Added bounded ingest queues, backpressure-triggered pipeline restart, hardware-encoder probes, exponential reconnect, segmented recording, clean shutdown, and optional MKV-to-MP4 remux.
- Selected recording variants can be produced even when their public provider output is disabled.

## Format and packaging

- Application, installer, Media Host, and web package version updated to `1.0.48`.
- Publication format updated to `1.45`.
