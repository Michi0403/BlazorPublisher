# PublisherStudio 3.9.5 source validation

## Scope

This is a source-only architecture-gate release. No .NET compilation was performed by the assistant.

## Async-only guard verification

`build/audit_async_only_architecture.py` was executed directly against `src/PublisherStudio.Web`.

Expected result: **non-zero**, because the new rule deliberately has no baseline and exposes pre-existing violations.

Observed inventory:

- 228 reviewed Component/Service/HostedService source files.
- 3,571 findings total.
- ASYNC001: 2,957 synchronous method signatures.
- ASYNC003: 351 computed getter/property workflow calls.
- ASYNC004: 3 `.Result` accesses.
- ASYNC005: 13 `GetAwaiter().GetResult()` calls.
- ASYNC006: 8 blocking wait calls.
- ASYNC008: 73 `lock` sites.
- ASYNC009: 14 `Interlocked`/`Volatile`/`Monitor` sites.
- ASYNC010: 2 synchronous coordination primitive sites.
- ASYNC011: 19 discarded async/fire-and-forget sites.
- ASYNC012: 33 Razor components missing `IAsyncDisposable`.
- ASYNC013: 37 Razor components missing `DisposeAsync`.
- ASYNC014: 12 `DisposeAsync` paths not calling private `Dispose()`.
- ASYNC015: 49 Razor components missing private `Dispose()`.

A synthetic positive/negative guard fixture was executed with Python to verify synchronous methods, computed getter calls, `.Result`, discarded async work, atomic coordination, the private `Dispose()` exception, `IAsyncDisposable`, and the `DisposeAsync -> Dispose()` requirement. The full repository audit completed in about five seconds in the source-only environment.

The PowerShell wrapper follows the repository's Windows PowerShell 5.1-compatible maintenance-guard structure and resolves `python`, `python3`, or `py -3` without invoking .NET.

The Razor maintenance audit was executed after removing its old contradictory "delegate computed property to a synchronous helper" requirement and passes for all 48 maintained Razor components. Procedural getter calls are owned by the async-only audit.

No `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online source access was used.
