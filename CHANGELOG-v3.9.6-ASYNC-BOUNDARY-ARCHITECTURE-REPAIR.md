# PublisherStudio 3.9.6 — async-boundary architecture repair

## Corrected maintenance architecture

- Repaired the 3.9.5 async-only maintenance gate instead of converting legitimate synchronous DevExpress/.NET/native contracts into artificial `Task` wrappers.
- Pure in-memory helpers, framework-mandated synchronous callbacks/overrides, bounded `lock`/`Interlocked`/`Volatile` state protection, synchronous native/process adapters, and components that own only synchronous disposal are valid again.
- Methods ending in `Async` must still expose awaitable contracts and `async void` remains forbidden.
- Properties/getters may not start asynchronous work. Renderer/awaitable orchestration paths may not use Task `.Result` or `GetAwaiter().GetResult()`.
- Blocking coordination (`Task.WaitAll/WaitAny`, `WaitOne`, `Thread.Sleep/Join`, blocking `Monitor` coordination) remains forbidden in maintained application workflows.
- Direct discarded asynchronous work remains forbidden. Intentional concurrency must be awaited or transferred to explicit ownership.
- Async disposal is required only when cleanup actually owns asynchronous resources/workers; the gate no longer requires empty `IAsyncDisposable` implementations on every Razor component.

## Audit correctness fixes

- `.Result` detection now requires a task-shaped receiver, so PublisherStudio command/result models are not confused with Task `.Result`.
- `_ => await ...` lambdas are no longer misread as `_ = ...` discard assignments.
- `_ = await SomeOperationAsync()` is recognized as an awaited operation whose result value is intentionally discarded.
- Short atomic/lock state protection is no longer treated as equivalent to blocking asynchronous coordination.
- The compatibility-preserving `Assert-AsyncOnlyArchitecture.ps1` filename remains wired into the normal build, but the underlying source audit now validates async boundaries rather than demanding asynchronous signatures for every helper/framework contract.

The corrected audit passes the maintained PublisherStudio source without requiring application-behavior rewrites. No DevExpress component ownership, render modes, installer workflow, streaming/native edge behavior, Panel Studio geometry, or persisted formats were intentionally changed.

No `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online repository access was used for this source handoff.
