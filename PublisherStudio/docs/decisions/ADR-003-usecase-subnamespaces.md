# ADR-003: UseCases are subnamespace orchestration

**Status:** Accepted

When controller or service areas grow, orchestration is split into `UseCases` beneath the owning architectural root, such as `Controllers/Streaming/UseCases` and `Services/Streaming/UseCases`. `UseCases` is not a top-level root and does not introduce command/handler/endpoint architecture.

Use cases coordinate process order. Reusable parsing, persistence, provider, FFmpeg, device, network and operating-system work remains in Services.
