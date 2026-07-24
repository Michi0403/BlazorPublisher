# PublisherStudio 1.0.62

## Architecture contract and streaming monolith restoration

- Added a root `AGENTS.md` repository contract for human and AI contributors. It fixes the accepted architectural roots, monolith-first rule, dependency direction, security boundaries and the rule that `UseCases` is an orchestration subnamespace rather than a competing top-level architecture.
- Added version-controlled system, streaming and interchange-format diagrams plus ADRs for monolith-first deployment, controller-owned application entry points, use-case subnamespaces and native/interchange format separation.
- Added a Picture/Video/Audio interchange capability matrix. Native PublisherStudio projects remain authoritative; future OpenRaster, OpenTimelineIO, Broadcast WAV and other adapters must validate in temporary state and report unsupported or flattened features before commit/export.

## Streaming backend rebuild

- Removed the former `Services/StreamingRuntime/StreamingRuntimeEndpoints.cs` aggregation and its main-host minimal API mapping.
- Restored every `/api/mediahost`, `/stream` and `/watch` application route as MVC controller actions under `Controllers/Streaming/UseCases` while preserving route and WebSocket compatibility.
- Split orchestration into focused service use-case namespaces for runtime information, native capture, sessions, Chat, ingest and LAN delivery.
- Moved FFmpeg, device capture, provider Chat, LAN/WebRTC/RTSP and metadata implementation into `Backend/Streaming` subnamespaces.
- Moved global hotkeys and Twitch OAuth maintenance into `HostedServices/Streaming`.
- Moved protected streaming profiles, publication-local streaming settings, Twitch OAuth, the in-process media facade and editor session orchestration into structured `Services/Streaming` subnamespaces.
- The in-process media facade now delegates runtime discovery and session lifecycle through service use cases instead of directly reaching into backend discovery or the session registry.
- Provider credentials, OAuth sessions, stream keys, LAN secrets and recording paths remain outside publication and interchange files.

## Validation

- Added an executable architecture contract suite that rejects forbidden top-level architecture roots, endpoint aggregation files, direct main-host minimal API routes and backend dependencies on MVC/controllers.
- Existing streaming route, OAuth, Chat, ingest, recording, native capture, FFmpeg, WebRTC, RTSP, LAN, hotkey and Now Playing contracts remain covered.

Application and installer version: `1.0.62`. Publication format remains `1.47`; picture format remains `1.2`.
