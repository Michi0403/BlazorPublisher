# PublisherStudio 4.2.2 — static source validation

Baseline: PublisherStudio 4.2.1 complete source archive.

- Baseline files retained: **1872/1872**.
- Modified previously existing files: **26**.
- Pre-existing new source files carried forward: **0**.
- Baseline ZIP CRC test: PASS.
- All maintained XML project/props/targets parsed successfully.
- Reviewed source-preservation/allowed-change boundary: PASS.
- No application release process was executed in this offline environment.

## Targeted contracts

- All 24 baseline synchronous lifecycle overrides converted to task-returning methods.
- Zero remaining synchronous Blazor lifecycle overrides in maintained Razor tree.
- Existing fast state updates remain fast (no fake async or forced ThreadPool dispatch).
- Editor recovery read now uses asynchronous service I/O and UI ConfigureAwait(true).
- Recovery service retains original synchronous API plus gated asynchronous equivalent using ConfigureAwait(false).
- TaskRunner-supervised background tasks and Parallel.ForEachAsync presence preserved.
- JS prerender boundary still active; async lifecycle/IO guard added.
- No Python files introduced or maintenance checks disabled.

## Runtime limitations

PowerShell 5.1, Visual Studio, DevExpress runtime, .NET compilation, and macOS release signing are not available in this container.
These checks **do not** establish a successful live Debug or Release build.
