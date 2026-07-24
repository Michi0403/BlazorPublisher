# PublisherStudio v1.0.62 release

See `CHANGELOG-v1.0.62.md`, `docs/architecture/system-overview.md`, `docs/architecture/streaming.md`, `docs/architecture/interchange-formats.md`, and `VALIDATION.md`.

This release restores the integrated streaming backend to PublisherStudio's established monolith architecture. Main-host media routes are MVC controllers again; process orchestration lives in `Services/Streaming/UseCases`; FFmpeg, native capture, provider Chat and LAN protocol implementations live in `Backend/Streaming`; and global hotkey/OAuth maintenance loops live in `HostedServices/Streaming`. The former `StreamingRuntimeEndpoints` aggregation and main-host minimal API mapping are removed without changing the browser route contract.

The source root now contains an explicit `AGENTS.md` architecture contract for human and AI contributors. It defines the approved roots, monolith-first rule, dependency direction and the accepted `UseCases` subnamespace pattern. Version-controlled Mermaid diagrams and ADRs document the system and streaming flows. A capability matrix defines how future Picture Studio, Video Studio and Audio Studio common formats remain adapters around the native PublisherStudio models rather than replacing them.

Application and installer version `1.0.62`; publication format `1.47`; picture format `1.2`. There is no separate Media Host executable or release payload.
