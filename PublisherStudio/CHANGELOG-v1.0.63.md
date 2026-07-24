# PublisherStudio 1.0.63

## Corrected backend entry and shared-service architecture

- Removed the duplicate top-level `Backend` folder and `PublisherStudio.Backend.*` namespaces.
- Defined Controllers as the request-driven start of the backend and Hubs as persistent-connection entry points.
- Added the explicit `Hubs/Streaming` root for `PlatformChatHub` and `WebRtcSignalingHub`.
- Moved reusable FFmpeg, native capture, provider Chat, WebRTC state, LAN/HLS/RTSP, metadata and runtime session implementation under focused `Services/Streaming` subnamespaces.
- Kept `UseCases` as orchestration beneath the existing Controller and Service areas rather than creating a competing root.

## HostedService reuse correction

- Moved global-hotkey processing and Windows interop into reusable `Services/Streaming/Hotkeys/GlobalHotkeyService`.
- Added a thin `HostedServices/Streaming/GlobalHotkeyHostedService` that only starts and stops the reusable service with the application lifecycle.
- Preserved Twitch OAuth maintenance as a scheduled HostedService that reuses profile and OAuth Services.
- Kept Components, Controllers, Hubs and HostedServices free to reuse Services directly in the Interactive Server monolith.

## Compatibility

- Preserved all `/api/mediahost`, `/stream`, `/watch`, Chat, ingest, WebRTC, recording, native-capture, LAN and stop-control route contracts.
- Preserved protected local streaming settings and credentials outside publication/template/interchange files.
- No publication, Picture Studio or interchange format version changed.

## Documentation and validation

- Rewrote `AGENTS.md`, system/streaming diagrams and ADRs to remove the old Backend-root instruction.
- Added ADR-005 for shared Service processing/I/O ownership.
- Architecture tests now reject a top-level `Backend` root, `PublisherStudio.Backend` namespaces and Services that depend on Components, Controllers, Hubs or HostedServices.

Application and installer version: `1.0.63`. Publication format remains `1.47`; picture format remains `1.2`.
