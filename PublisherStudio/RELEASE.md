# PublisherStudio v1.0.64 release

See `CHANGELOG-v1.0.64.md`, `AGENTS.md`, `docs/architecture/system-overview.md`, `docs/architecture/streaming.md`, ADR-006 and `VALIDATION.md`.

This release removes the duplicate `MediaHostHotkeyEvent` left by the v1.0.63 streaming reorganization. The global-hotkey service, session registry, streaming use cases, in-process media-host facade and editor now share the single canonical record declared under `Domain/Streaming`. The facade no longer creates a same-named Service-local copy, resolving the reported CS0104 ambiguities and the related source/assembly CS1503 conversion failure.

The repository contract now explicitly requires one authoritative owner for every shared semantic contract. Services may not shadow Domain/Models types. Separate DTOs are reserved for real provider, protocol, serialization or process boundaries and must be distinctly named and mapped once. A new executable Node suite scans C# declarations, Domain/Models-to-Services shadowing and the combined symbol scope created by `GlobalUsings*.cs`.

Application and installer version `1.0.64`; publication format `1.47`; picture format `1.2`. Streaming routes and runtime behavior are unchanged. Source archives contain no build output; clean an existing v1.0.63 checkout before rebuilding so Visual Studio discards any design-time assembly containing the removed duplicate type.
