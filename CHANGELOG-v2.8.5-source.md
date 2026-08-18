# PublisherStudio 2.8.5 source changelog

## Scope

PublisherStudio 2.8.5 is a compile-repair and architecture-maintenance release over 2.8.4. It ports the strict LocalGPT continuation and asynchronous-ownership maintenance pattern to PublisherStudio without redesigning the editor, canvas, layer, media, or render-mode architecture.

## Compile repairs

- Restored `PageSurface.FitPageAsync()` because `Editor.razor` contains two reviewed calls to that public surface method.
- Repaired the 2.8.4 discarded-async finding in `DevExtremeComponentEditor` and the other intentionally concurrent component/service call sites by transferring ownership to `ISupervisedTaskRunner` instead of discarding returned tasks.

## Method-local resilience

- Every discovered PublisherStudio Razor/component method now owns a method-local exception boundary and structured logging.
- Normal component methods use `try/catch + ILogger` diagnostics.
- Every PublisherStudio service method is checked with zero PublisherStudio exclusions; normal service methods require `try/catch + ILogger/Trace` diagnostics.
- Iterator/yield methods are checked across maintained C# and Razor source and require `try/finally + diagnostics` with `catch` forbidden inside the iterator method.
- The previous component and iterator baseline/grandfather files were removed. There is no exemption path for new or existing PublisherStudio component/service/iterator methods.

## LocalGPT-compatible async continuation policy

- PublisherStudio now uses the same syntax-aware `audit_async_continuations.py` implementation as LocalGPT 3.0.9.
- Every maintained ordinary `await` must explicitly end in `ConfigureAwait(false)` or `ConfigureAwait(true)`.
- `ConfigureAwait(false)` is the mandatory default.
- `ConfigureAwait(true)` is forbidden outside Components and, inside Components, is accepted only for the three reviewed Blazor lifecycle methods or an exact helper method listed in `build/async-continuation-policy.json`.
- `await foreach` must use `ConfigureAwait(false)` and async disposal must be explicit.
- Renderer-affine work moved through the supervised runner is marshalled through `InvokeAsync` where required; background/service work remains free-threaded with `ConfigureAwait(false)`.

## Asynchronous ownership

- Added `ISupervisedTaskRunner` / `SupervisedTaskRunner` and singleton DI registration.
- Intentionally concurrent work is tracked and observed instead of `_ =` discarding Task-returning calls.
- `Assert-ServiceArchitecture.ps1` now protects the supervised-runner registration and rejects discarded asynchronous work or manual construction of the runner.

## Preserved behavior

- No intentional change to the five reviewed `InteractiveServer` boundaries.
- LocalGPT 1-Wire protocol remains `2.1.1`.
- Video Studio effects/layers and rendered-video export remain present.
- Adaptive media quality, modal canvas interaction suspension, slider coalescing, Converter Studio recommendations, Story Editor recovery, and Panel Studio behavior are retained.
- No EF migration or storage-schema change was introduced.

## Build scope

No `dotnet`, MSBuild, restore, build, publish, or pack command was run while preparing this source release. The consumer's .NET build remains authoritative for compiler/reference validation.
