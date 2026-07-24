# PublisherStudio 1.0.66

## Compiler fixes reintegrated

Michael's uploaded working tree was compared file-by-file with the clean v1.0.65 source. Three application-source fixes were present and have been reintegrated:

- `Program.cs` now imports `PublisherStudio.HostedServices.Streaming`, making the moved `TwitchOAuthMaintenanceService` registration compiler-visible.
- `PlatformChatService` qualifies UTF-8 through `global::System.Text.Encoding`.
- `RtspLanServer` qualifies ASCII and UTF-8 through `global::System.Text.Encoding`.

The qualification is necessary because `PublisherStudio.Services.Streaming.Encoding` is a sibling namespace. Within other `PublisherStudio.Services.Streaming.*` namespaces, the simple identifier `Encoding` can resolve to that namespace instead of the BCL type.

No `.git`, `.vs`, `.cr`, `node_modules`, `bin`, `obj`, IDE personalization, or generated build output was copied from the uploaded working tree.

## Architecture and AI rules

`AGENTS.md`, `docs/ARCHITECTURE.md`, the system overview, and ADR-008 now state that namespace names are compiler-visible architecture:

- New namespace leaves must avoid collisions with visible framework/project type names.
- Existing compatibility collisions use `global::` qualification or an explicit alias.
- Moving a Service, Hub, HostedService, Controller, or contract requires the corresponding composition-root import/qualification in the same change.
- A real `dotnet build` must be run when the SDK and licensed package feed are available. Static checks must not be reported as a compiler build.

## Executable prevention

A new `csharpCompilationSafety.test.mjs` suite:

- Resolves project types used in dependency-injection registrations inside `Program.cs` and `*ServiceCollectionExtensions.cs` and verifies that their namespaces are visible.
- Detects common framework identifiers used unqualified when a sibling project namespace shadows the same simple name.
- Locks the concrete `HostedServices.Streaming` import and `global::System.Text.Encoding` repairs.

The aggregate package test now runs 16 suites.

## Compatibility

No runtime feature, route, publication schema, Picture Studio schema, export path, or dependency changed. Publication format remains `1.48`; Picture Studio format remains `1.3`.

Application and installer version: `1.0.66`.
