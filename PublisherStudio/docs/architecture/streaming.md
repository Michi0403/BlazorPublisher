# Streaming architecture

Streaming remains part of `PublisherStudio.Web`; it is not a separately deployed Media Host.

## Component structure

```mermaid
flowchart LR
    UI[Streaming Studio / Editor] --> ES[StreamingSessionService use-case orchestration]
    ES --> MF[StreamingMediaHostClient facade]
    MF --> SU[Streaming session use cases]
    MC[MVC streaming controllers] --> RU[Runtime / capture / chat / ingest / LAN use cases]
    RU --> SR[MediaSessionRegistry]
    SU --> SR
    SR --> BE[Backend streaming implementations]
    HS[Global hotkey and Twitch OAuth hosted services] --> SR
    HS --> PS[Protected profile/OAuth services]
```

## Main-host transport ownership

All `/api/mediahost`, `/stream` and `/watch` application routes are MVC controller actions under `Controllers/Streaming/UseCases`. The former `StreamingRuntimeEndpoints` aggregation no longer exists.

Controllers own HTTP status codes, model binding and WebSocket upgrade/close behavior. Services under `Services/Streaming/UseCases` own session lookup and operation ordering. Backend code owns FFmpeg, capture devices, chat providers, HLS/RTSP/WebRTC implementation and metadata parsing.

## Start session

```mermaid
sequenceDiagram
    participant UI as Streaming UI
    participant E as StreamingSessionService
    participant F as StreamingMediaHostClient
    participant U as StreamingSessionUseCases
    participant R as MediaSessionRegistry
    participant B as Backend encoders/providers

    UI->>E: Start(document, dryRun)
    E->>F: Start(document, dryRun)
    F->>F: Resolve protected local profiles and OAuth
    F->>U: Create(normalized runtime request)
    U->>R: Create(request)
    R->>B: Configure chat, LAN, hotkeys and encoder state
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
    participant B as Encoder backend

    UI->>E: Stop streaming
    loop enabled provider outputs
        E->>F: Set output disabled
        F->>U: SetOutput(false)
        U->>R: SetOutput(false)
        R->>B: Stop provider pipeline
    end
    E-->>UI: Recording-only/session-active status
```

## Complete stop

A complete stop cancels UI event polling, removes the session from the registry, unregisters global hotkeys, closes encoder input, completes ingest subscribers, disposes provider Chat, closes WebRTC peers and disposes LAN servers. Recording-only and provider-only stop operations remain separate.
