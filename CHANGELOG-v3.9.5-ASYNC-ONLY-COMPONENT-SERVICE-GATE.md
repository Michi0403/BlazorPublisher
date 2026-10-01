# PublisherStudio 3.9.5 — async-only component/service architecture gate

## Architecture rule

- Components, Services, and HostedServices are zero-baseline asynchronous ownership boundaries. Declared methods must return awaitable/async-stream contracts rather than synchronous `void` or value-returning helpers.
- The only synchronous method exception is `private void Dispose()`. Every Razor component must implement `IAsyncDisposable`, declare `DisposeAsync`, and at minimum call its private `Dispose()` helper from `DisposeAsync`.
- Computed properties/getters may not invoke workflow helpers synchronously; awaited lifecycle/service work must materialize state before rendering/consumption.
- `.Result`, blocking waits, `GetAwaiter().GetResult()`, `Thread.Sleep`/`Join`, synchronous locking/atomic coordination, and discarded async/fire-and-forget starts are rejected in Components and Services.
- Long-lived work must be explicitly owned by an async worker/hosted service or retained and awaited through async disposal rather than coordinated through atomics plus recursive discarded tasks.

## Guard

- Added `build/Assert-AsyncOnlyArchitecture.ps1` and `build/audit_async_only_architecture.py`.
- Wired the guard directly after async-continuation validation and before component diagnostics in normal `PublisherStudio.Web` builds.
- Removed the contradictory Razor-maintenance rule that previously required expression-bodied computed properties to delegate to synchronous helper methods. The Razor guard remains focused on layout/diagnostics while the async-only guard owns getter/workflow-call enforcement.
- There is intentionally no normal-build skip switch and no grandfathered baseline. All existing findings are emitted with source file/line diagnostics for the migration pass.
- The audit remains bounded to maintained Component/Service/HostedService source and completes in about five seconds in the assistant's source-only environment.

## Initial source audit

The source-only audit currently reports **3,571 existing findings across 228 reviewed Component/Service/HostedService files**. This is the requested migration inventory, not a passing-baseline claim.

- `ASYNC001`: 2,957 synchronous method signatures.
- `ASYNC003`: 351 computed getter/property workflow calls.
- `ASYNC004`: 3 `.Result` uses.
- `ASYNC005`: 13 `GetAwaiter().GetResult()` uses.
- `ASYNC006`: 8 blocking wait calls.
- `ASYNC008`: 73 `lock` uses.
- `ASYNC009`: 14 `Interlocked`/`Volatile`/`Monitor` coordination calls.
- `ASYNC010`: 2 synchronous coordination primitive uses.
- `ASYNC011`: 19 discarded asynchronous invocations.
- `ASYNC012`: 33 Razor components missing explicit `IAsyncDisposable` ownership.
- `ASYNC013`: 37 Razor components missing `DisposeAsync`.
- `ASYNC014`: 12 existing `DisposeAsync` paths that do not call the required private `Dispose()` helper.
- `ASYNC015`: 49 Razor components missing the private `Dispose()` cleanup helper.

No PublisherStudio application behavior was intentionally refactored in 3.9.5; this release establishes the same repository rule as LocalGPT and exposes its existing debt for controlled follow-up conversion.

No `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online repository access was used for this source handoff.
