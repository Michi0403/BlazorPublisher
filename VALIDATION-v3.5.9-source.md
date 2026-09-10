# PublisherStudio 3.5.9 source validation

Source-only validation was performed in the delivery environment. Per the maintenance constraint, no GitHub access and no `dotnet build` / `dotnet publish` were used. The reported Visual Studio rebuild is treated as the compiler evidence for 3.5.8; this release repairs the exact documentation warnings/findings in source and validates them with the repository's maintained static audits.

Validated invariants:

- PublisherStudio web and installer project versions, npm package identity, documentation identity, PDF identity, and browser cache-busters are 3.5.9; minor and patch slots remain single-digit.
- `StopInstalledPublisherStudioForUpdate` has XML `<param>` documentation for exactly its current parameters: `installRoot`, `runtimeFolderName`, and `logger`. Stale `applicationZipPath`, `setupZipPath`, `targetPath`, and `runtimeIdentifier` tags are absent from that method's documentation.
- `FileLoggerSharedSink.Dispose` and `FileLoggerProvider.Dispose` carry specific ownership/drain semantics and no longer match the maintained generic-summary rejection rule.
- No production method body was modified for this maintenance fix; the source changes in the three affected C# files are documentation-only.
- The maintained logging baseline is unchanged and the shared one-queue/one-writer design from 3.5.8 remains present.
- The renderer-affinity audit policy and Story Editor one-shot browser-layout attachment repair remain present.
- The LocalGPT-aligned `%LOCALAPPDATA%\\PublisherStudio` overlay installer calls remain present and the forbidden transactional/whole-directory replacement dialect remains absent.
- The early installer compilation preflight remains before documentation/native packaging.
- The exact Python target behind `Assert-XmlDocumentationCoverage.ps1` passes: 6,340 direct C# declarations across 253 maintained source files and 3,445 direct Razor `@code` declarations across 48 component types.
- Application architecture, async continuation, component resilience, prerender JavaScript interop, service resilience, iterator exception, cross-platform boundary, and Panel Studio persistence audits pass. The async audit remains at 1,102 await tokens: 429 `ConfigureAwait(false)`, 619 renderer-affine `ConfigureAwait(true)`, 49 explicitly configured await-using disposals, and 5 configured async streams.
- Direct emulation of the committed logging-integrity baseline passes for 102 maintained files without modifying `build/logging-baseline.json`.
- JSON syntax validation passes for `package.json`, `package-lock.json`, and `docs/docfx.json`.

A new .NET compiler result is intentionally not claimed because no .NET SDK build was run in this environment.
