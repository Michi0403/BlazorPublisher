# PublisherStudio repository contract

These rules are part of the source architecture. They apply to human and AI contributors.

## Stable architectural roots

PublisherStudio uses the existing solution roots and their subnamespaces:

- `Components` for Blazor UI and UI coordination
- `Controllers` for application HTTP/WebSocket entry points
- `Services` for application capabilities, persistence and orchestration
- `HostedServices` for long-running application-owned work
- `Backend` for FFmpeg, provider, device, protocol and operating-system implementations
- `Domain` and `Models` for authoritative documents, contracts and view models

Do not introduce competing top-level application patterns such as `Endpoints`, `Features`, `Handlers`, `Commands`, `Queries`, `UseCases`, `Infrastructure` or `Application` unless Michael explicitly approves an architecture change.

## Use-case orchestration

Large areas may use a `UseCases` subnamespace beneath an established root. This is the approved way to stop controllers and services becoming monolithic.

Allowed examples:

- `Controllers/Streaming/UseCases`
- `Services/Streaming/UseCases`
- `Services/PictureStudio/UseCases`
- `Services/VideoStudio/UseCases`
- `Services/AudioStudio/UseCases`

`UseCases` must never become a new top-level root. A use case coordinates existing capabilities and transaction/process order. Low-level provider, persistence, FFmpeg, rendering, protocol and operating-system details remain in `Backend` or their owning service.

## Dependency direction

The intended direction is:

```text
Components -> Controllers / service use cases -> Services -> Backend
                         |                    -> Domain
HostedServices -> service use cases / Services -> Backend
```

Controllers must stay transport-focused and delegate orchestration. Blazor components must not parse interchange formats or own backend processes. Backend code must not depend on Blazor components or MVC controllers.

## Monolith first

PublisherStudio is an Interactive Blazor Server desktop/local-network monolith. Keep a capability inside the monolith unless a real process, deployment, crash-isolation, scaling or incompatible-dependency boundary requires a separate program. Do not introduce a microservice or microfrontend merely because a framework example uses one.

## HTTP and WebSocket routes

Main application routes belong to MVC controllers under `Controllers`. Do not add main-host `MapGet`, `MapPost`, `MapPut`, `MapDelete` or a `*Endpoints.cs` aggregation.

A protocol-specific listener fully owned by a backend implementation, such as the isolated LAN playback listener, may register its private transport routes inside that backend. It must not become an alternative business/application routing layer.

## Streaming security

Provider tokens, OAuth sessions, stream keys, LAN secrets, recording destinations and machine-specific streaming configuration must not be stored in publications, templates or interchange exports. Keep them in the existing protected local stores.

## Interchange formats

The native PublisherStudio project model remains authoritative. External formats are adapters:

```text
external file -> parser -> temporary canonical model -> validation/loss report -> commit
canonical model -> capability analysis -> mapping -> external writer
```

Imports must not mutate the active project before validation succeeds. Exporters must report unsupported, flattened and lossy features. Do not reshape the native model around a third-party format.

## Before adding or moving code

1. Inspect the closest existing implementation.
2. Follow its root, subnamespace and dependency direction.
3. Reuse existing abstractions where they fit.
4. Keep public behavior and serialized formats compatible unless the task explicitly changes them.
5. Add or update architecture and behavior tests.
6. Do not create a new architectural dialect to mirror a tutorial or library sample.
