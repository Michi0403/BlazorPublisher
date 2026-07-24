# Streaming architecture

Streaming remains part of `PublisherStudio.Web`; it is not a separately deployed Media Host and it has no separate `Backend` architectural root.

## Component structure

```mermaid
flowchart LR
    UI[Streaming Studio / Editor] --> ES[StreamingSessionService use-case]
    ES --> MF[StreamingMediaHostClient facade]
    MF --> SU[Streaming service use cases]
    MC[MVC streaming controllers] --> SU
    CH[PlatformChatHub] --> CU[Chat service use cases]
    WH[WebRtcSignalingHub] --> IU[Ingest service use cases]
    SU --> SR[MediaSessionRegistry]
    CU --> SR
    IU --> SR
    SR --> SS[Capture / encoding / chat / LAN / metadata services]
    GH[GlobalHotkeyHostedService] --> GS[GlobalHotkeyService]
    TH[TwitchOAuthMaintenanceService] --> PS[Protected profile and OAuth services]
```

## Entry points and shared processing

All `/api/mediahost`, `/stream` and `/watch` main-application routes are MVC controller actions under `Controllers/Streaming/UseCases`. The former `StreamingRuntimeEndpoints` aggregation no longer exists.

Persistent Chat and WebRTC connection entry classes live under `Hubs/Streaming`:

- `PlatformChatHub` owns the platform-Chat WebSocket entry role.
- `WebRtcSignalingHub` owns the renderer-side WebRTC signaling entry role.

Controllers and Hubs own transport negotiation and connection lifecycle. Shared work is under `Services/Streaming`: session state, FFmpeg and encoder orchestration, native capture, provider Chat adapters, LAN/HLS/RTSP processing, WebRTC signaling state, metadata parsing, hotkeys, OAuth and protected stores.

Hosted services are lifecycle adapters. `GlobalHotkeyHostedService` starts and stops the reusable `GlobalHotkeyService`; `TwitchOAuthMaintenanceService` schedules validation through the reusable OAuth/profile Services. Services do not depend on HostedServices, Controllers, Hubs or Components.

## Start session

```mermaid
sequenceDiagram
    participant UI as Streaming UI
    participant E as StreamingSessionService
    participant F as StreamingMediaHostClient
    participant U as StreamingSessionUseCases
    participant R as MediaSessionRegistry
    participant S as Streaming Services

    UI->>E: Start(document, dryRun)
    E->>F: Start(document, dryRun)
    F->>F: Resolve protected local profiles and OAuth
    F->>U: Create(normalized runtime request)
    U->>R: Create(request)
    R->>S: Configure Chat, LAN, hotkeys and encoder state
    R-->>U: MediaSession
    U-->>F: MediaSession
    F-->>E: Session response
    E-->>UI: Active snapshot
```

## Stop provider streaming without stopping recording

```mermaid
sequenceDiagram
    participant UI as Ribbon
    participant E as StreamingSessionService
    participant F as StreamingMediaHostClient
    participant U as StreamingSessionUseCases
    participant R as MediaSessionRegistry
    participant S as Encoder service

    UI->>E: Stop streaming
    loop enabled provider outputs
        E->>F: Set output disabled
        F->>U: SetOutput(false)
        U->>R: SetOutput(false)
        R->>S: Stop provider pipeline
    end
    E-->>UI: Recording-only/session-active status
```

## Complete stop

A complete stop cancels UI event polling, removes the session from the registry, unregisters global hotkeys, closes encoder input, completes ingest subscribers, disposes provider Chat services, closes WebRTC peers and disposes LAN services. Recording-only and provider-only stop operations remain separate.
