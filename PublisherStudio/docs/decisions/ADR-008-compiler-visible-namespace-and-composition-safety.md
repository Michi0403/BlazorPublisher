# ADR-008: Compiler-visible namespace and composition-root safety

## Status

Accepted for PublisherStudio v1.0.66; corrected and strengthened for v1.0.67.

## Context

After the v1.0.65 source was integrated into a real Visual Studio checkout, three compiler fixes were required:

- `Program.cs` referenced `TwitchOAuthMaintenanceService` after it had moved under `PublisherStudio.HostedServices.Streaming`, but the composition root did not import that namespace.
- `PlatformChatService` and `RtspLanServer` used `Encoding` as a simple framework type name while the enclosing `PublisherStudio.Services.Streaming` namespace also contained a child namespace named `Encoding`.

C# resolves names through enclosing namespace members. Therefore a sibling namespace can shadow a type imported through `using System.Text`, even though the folder arrangement looks correct. Static delimiter and architecture checks did not detect either issue.

## Decision

1. Namespace and folder names are treated as compiler-visible architecture, not presentation-only organization.
2. New namespace leaves must avoid simple names that collide with framework or project types visible from the same enclosing namespace.
3. Existing collisions retained for compatibility must use a deliberate file-level alias or `global::` qualification at every affected reference. A file-level alias is preferred for repeated use and for interpolation expressions.
4. Composition-root registrations must explicitly import or qualify the namespace that owns each project type. Moving a Service, Hub, HostedService, Controller or contract includes updating the composition root in the same change.
5. `global::` must not appear directly at the start of an interpolation hole. In `$"{global::Type.Member}"`, the interpolation parser treats the colon as a format separator and attempts to resolve an identifier named `global`. Use an alias, a local variable, or parenthesize the expression.
6. `tests/csharpCompilationSafety.test.mjs` checks project DI registrations, sibling-namespace shadowing, explicit aliases for the known Streaming collision, and unsafe `{global::` interpolation holes.
7. A real `dotnet build` is required whenever the required SDK and licensed DevExpress feed are available. Static checks are fallback evidence only and must never be described as a successful compiler build.

## Consequences

The existing `PublisherStudio.Services.Streaming.Encoding` namespace remains to avoid a broad compatibility rename. Streaming siblings use a deliberate alias such as `TextEncoding = global::System.Text.Encoding`. Future areas should prefer precise capability names that do not collide with framework types. Alias-qualified or local values keep interpolation syntax unambiguous.

Architecture refactors gain one additional completion criterion: the composition root and compiler-visible names must be validated together with dependency direction and contract ownership.
