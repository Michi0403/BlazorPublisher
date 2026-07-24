# PublisherStudio v1.0.63 release

See `CHANGELOG-v1.0.63.md`, `AGENTS.md`, `docs/architecture/system-overview.md`, `docs/architecture/streaming.md`, and `VALIDATION.md`.

This release corrects the repository architecture contract and streaming placement without changing the public streaming route or publication formats. Controllers are request-driven backend entry points, `Hubs` owns persistent platform-Chat and WebRTC connection entry roles, and reusable processing/technical I/O is organized beneath `Services`. The separate `Backend` root no longer exists.

FFmpeg orchestration, native capture, provider Chat adapters, LAN/HLS/RTSP/WebRTC state, metadata parsing, hotkeys and media sessions now live under structured `Services/Streaming` subnamespaces. `GlobalHotkeyService` is reusable from Controllers, Components, Hubs and other services; `GlobalHotkeyHostedService` only owns application startup/shutdown. The repository instructions and architecture tests enforce the same direction for future human and AI changes.

Application and installer version `1.0.63`; publication format `1.47`; picture format `1.2`. There is no separate Media Host executable or release payload.
