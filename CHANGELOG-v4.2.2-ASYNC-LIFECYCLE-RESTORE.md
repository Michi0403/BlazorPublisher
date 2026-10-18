# PublisherStudio 4.2.2 — Async component lifecycle and recovery I/O

Base: the full PublisherStudio 4.2.1 source archive. LocalGPT was not included or modified.

## Component/service orchestration ownership

- Migrated the 24 remaining synchronous `OnInitialized`, `OnParametersSet`, and `OnAfterRender` overrides to task-returning Blazor lifecycle overrides. Fast component state setup, subscriptions, conditional early exits, and existing service calls stay in place rather than being dispatched to `Task.Run` or wrapped in fake `Task.Yield`.
- Replaced the Editor's synchronous crash-recovery read on initialization with an **awaited** injected `PublicationRecoveryService.TryReadLatestAsync`. This method uses `SemaphoreSlim.WaitAsync` and `File.ReadAllTextAsync` to avoid blocking an InteractiveServer circuit and to coordinate with the existing atomic `SaveAsync`. The existing synchronous service API stays available to other clients.
- The Editor's asynchronous UI lifecycle uses `ConfigureAwait(true)` to retain the render context. The recovery service uses `ConfigureAwait(false)` for I/O-only continuations. Those choices are intentional and are **not** normalized globally.
- Preserved existing supervised fire-and-forget paths (including `TaskRunner` and task-owned cancellation/disposal), existing `Parallel.ForEachAsync` work, and caller-controlled await policy; **no forced serialization of independent work**.
- `Assert-ComponentSafety.ps1` now fails future synchronous component lifecycle overrides and synchronous Editor recovery calls, while still checking real prerender-sensitive JS interop. It does not forbid supervised background work or alter cross-platform/DevExpress/diagnostics guards.

## Scope

No application service other than the recovery service and no business logic beyond task-entrypoint lifecycle conversion was changed. Original Python-free maintenance system, installation/release scripts, visual rendering and third-party code were preserved.

See `VALIDATION-v4.2.2-source.md` for static evidence and runtime limitations. No local PowerShell/.NET/Visual Studio build is claimed.
