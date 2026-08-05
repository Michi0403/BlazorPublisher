# PublisherStudio 2.1.6

## Development exception diagnostics

- Added a development-only, DI-owned first-chance exception observer.
- PublisherStudio-originated exceptions now carry application logging even when a component later handles them.
- Expected cancellation, disposal, and disconnected-circuit exceptions are logged at Debug level.
- Invalid operation exceptions originating in framework lifecycle code are logged at Debug level during development.
- Unexpected PublisherStudio exceptions are logged at Warning level with their exception and call site.
- Repeated exceptions are bounded by call-site fingerprint and summarized instead of flooding the log.
- Host shutdown cancellation, unexpected host termination, and runtime-endpoint cleanup failures now have explicit logs.
- Release logging remains unchanged; first-chance observation is active only in the Development environment.

No application static state or static convenience logger was introduced. Diagnostic options are configuration-backed BusinessObjects, and observation is owned by an injected hosted service.
