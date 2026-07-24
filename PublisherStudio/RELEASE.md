# PublisherStudio v1.0.66 release

See `CHANGELOG-v1.0.66.md`, `AGENTS.md`, ADR-008, `docs/ARCHITECTURE.md`, `docs/architecture/system-overview.md`, and `VALIDATION.md`.

This release reintegrates the compiler fixes found in Michael's uncommitted working tree on top of the complete v1.0.65 source. The clean source package excludes `.git`, `.vs`, `.cr`, `node_modules`, `bin`, and `obj`.

`Program.cs` now imports `PublisherStudio.HostedServices.Streaming` explicitly so the moved `TwitchOAuthMaintenanceService` registration is compiler-visible. `PlatformChatService` and `RtspLanServer` use `global::System.Text.Encoding` because the existing sibling namespace `PublisherStudio.Services.Streaming.Encoding` can shadow the BCL type during C# name lookup.

The repository contract now treats namespace names and composition-root imports as compiler-visible architecture. `csharpCompilationSafety.test.mjs` checks DI registration visibility and common framework-type shadowing. This supplements a real .NET build; it does not replace one.

Application and installer version is `1.0.66`. Publication format remains `1.48`, Picture Studio format remains `1.3`, and no NuGet/npm/native dependency changed.
