# PublisherStudio 3.5.8 source validation

Source-only validation was performed in the delivery environment. Per the request, no GitHub access and no `dotnet build` / `dotnet publish` were used. PowerShell is not installed in the delivery environment, so PowerShell-only guards were not claimed as executed; the reported failing logging guard was reproduced directly from its committed baseline/metric rules.

Validated invariants:

- PublisherStudio web and installer project versions, npm package identity, documentation identity, and browser cache-busters are 3.5.8; the minor and patch slots remain single-digit.
- `PublisherStudio.InstallerConsole/Helper/SetupFileLoggerProvider.cs` explicitly imports `System` and `System.IO`, covering the unresolved BCL types from the reported build: `Environment`, `DateTimeOffset`, `IDisposable`, `Exception`, `Func<,,>`, `Path`, `Directory`, and `File`.
- The maintained logging baseline was not reduced. `FileLogger.cs` now satisfies its baseline with 3 `ILogger` references, 0 direct `Log*` calls, and 11 catch tokens against minima 3/0/10. `FileLoggerProvider.cs` satisfies 1/0/3 exactly.
- PublisherStudio still owns exactly one shared file-writer thread construction in `FileLoggerSharedSink`; no per-category writer/queue design was restored.
- The added `FileLoggerProvider` constructor boundary logs shared-sink initialization failure and rethrows, preserving failure visibility without swallowing startup faults.
- Async continuation audit passes across 80 source files: 1,102 await tokens, 429 `ConfigureAwait(false)`, 619 renderer-affine `ConfigureAwait(true)`, 49 configured await-using disposals, and 5 configured async streams.
- Component resilience and prerender interop audits pass across 2,687 component methods; 13 JavaScript-aware disposal methods remain attachment-gated.
- Service resilience passes for 1,382 ordinary service methods plus 3 iterator/yield methods; iterator exception policy also passes.
- Cross-platform boundary audit passes with 60 checks; Panel Studio persistence audit passes; the combined application architecture audit passes.
- Story Editor keeps the 3.5.7 one-shot browser-layout attachment guard and renderer-affinity policy.
- The LocalGPT-aligned `%LOCALAPPDATA%\PublisherStudio` overlay installer calls remain present, the forbidden transactional/whole-directory replacement dialect remains absent, and the installer compile preflight remains before documentation/native packaging.
- JSON syntax validation passes for `package.json`, `package-lock.json`, and `docs/docfx.json`.

A real compiler result is intentionally not claimed because no .NET SDK build was run in this environment.
