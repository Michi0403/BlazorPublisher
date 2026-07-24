# PublisherStudio repository contract

These rules are part of the source architecture. They apply to human and AI contributors.

## Stable architectural roots

PublisherStudio uses these existing solution roots and their subnamespaces:

- `Components` for Blazor frontend state, display and UI coordination
- `Controllers` for request/response entry points; Controllers start the backend for normal HTTP and WebSocket requests
- `Hubs` for persistent connection entry points and connection-specific coordination
- `Services` for reusable application capabilities, general data processing, persistence and technical I/O such as files, network communication, FFmpeg, devices and operating-system APIs
- `HostedServices` for application-lifetime scheduling, polling and start/stop lifecycle adapters
- `Domain` and `Models` for authoritative documents, shared contracts and view models

There is no separate `Backend` architectural root. Controllers and Hubs are backend entry points; backend work behind those entry points is implemented as reusable Services.

Do not introduce competing top-level application patterns such as `Backend`, `Endpoints`, `Features`, `Handlers`, `Commands`, `Queries`, `UseCases`, `Infrastructure` or `Application` unless Michael explicitly approves an architecture change.

## Shared service rule

Services are reusable by Components, Controllers, Hubs and HostedServices. Frontends may inject Services directly when no HTTP boundary is needed in the Interactive Server monolith.

Services must not depend on Components, Controllers, Hubs or HostedServices. Keep reusable work in Services and keep the callers thin:

- Controllers own model binding, HTTP results, authorization decisions and WebSocket negotiation.
- Hubs own persistent-connection entry and connection lifecycle.
- HostedServices own scheduling and application lifetime, then call Services for the actual work.
- Components own user interaction and UI state, then call Services or service use cases.

If logic is useful from more than one caller, it belongs in Services rather than being copied into a Controller, Hub, HostedService or Component.

## Use-case orchestration

Large controller or service areas may use a `UseCases` subnamespace beneath the existing owning root. This is the approved way to stop controllers and services becoming monolithic.

Allowed examples:

- `Controllers/Streaming/UseCases`
- `Services/Streaming/UseCases`
- `Services/PictureStudio/UseCases`
- `Services/VideoStudio/UseCases`
- `Services/AudioStudio/UseCases`

`UseCases` must never become a new top-level root. A use case coordinates existing capabilities and process order. Technical parsing, storage, provider, FFmpeg, device, protocol and operating-system work remains in the relevant Service subnamespace.

## Dependency direction

The intended direction is:

```text
Components -------> Services / service use cases -------> Domain / Models
Controllers ------> Services / service use cases -------> Domain / Models
Hubs -------------> Services / service use cases -------> Domain / Models
HostedServices ---> Services / service use cases -------> Domain / Models
```

A composition-root helper beside `Program.cs` may register all roots in dependency injection. It is wiring, not business processing.

## Monolith first

PublisherStudio is an Interactive Blazor Server desktop/local-network monolith. Keep a capability inside the monolith unless a real process, deployment, crash-isolation, scaling or incompatible-dependency boundary requires a separate program. Do not introduce a microservice or microfrontend merely because a framework example uses one.

## HTTP, WebSocket and protocol routes

Main application HTTP and WebSocket routes belong to MVC controllers under `Controllers` or connection entry classes under `Hubs`. Do not add main-host `MapGet`, `MapPost`, `MapPut`, `MapDelete` or a `*Endpoints.cs` aggregation.

A private protocol listener created by a Service, such as the isolated LAN playback host, may expose only its own transport routes. It must not become a second business/application architecture and its reusable processing still belongs in Services.

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
3. Reuse existing Services where they fit.
4. Keep public behavior and serialized formats compatible unless the task explicitly changes them.
5. Add or update architecture and behavior tests.
6. Do not create a new architectural dialect to mirror a tutorial or library sample.
