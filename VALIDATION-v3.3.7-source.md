# PublisherStudio 3.3.7 source validation

Static validation covers version consistency, restored package-only consumption of `LocalGPT.ReleasePackaging` 1.0.2, the absence of duplicate LocalGPT helper source, adaptive memory-based browser PDF chunking, managed PDF merge/optimization, compressed embedded documentation PDF delivery, Full/self-contained Unix/macOS packaging, resumable notarization/reuse, Homebrew RPM support, architecture/cross-platform boundaries, async/service resilience, XML documentation, and the four reviewed InteractiveServer boundaries.

The release-packaging resolver may copy the authoritative package from a LocalGPT checkout/shared cache or download the matching LocalGPT release asset; it does not run `dotnet pack` against LocalGPT source inside PublisherStudio. NuGet.org is retained only as a dependency source when `dotnet tool install` resolves PDFsharp for the already-built tool package.

Source checks completed in this environment:

- current release audit: passed;
- application architecture audit: passed;
- cross-platform boundary audit: 60 checks passed;
- async continuation audit: 80 source files, 1102 await tokens, 465 `ConfigureAwait(false)`, 583 renderer-affine `ConfigureAwait(true)`, 49 configured `await using` disposals, and 5 configured async streams;
- service resilience audit: 1376 service methods own try/catch + diagnostics and 3 iterator/yield methods own try/finally + diagnostics;
- XML documentation: 6,287 direct C# declarations across 252 maintained source files plus 48 Razor component types / 3,443 direct `@code` members passed;
- duplicate `src/LocalGPT.ReleasePackaging`: absent and guarded by the current release audit;
- `@rendermode InteractiveServer`: 4 occurrences, unchanged;
- repository-local `bin`/`obj`: none before source packaging.

This environment does not contain PowerShell or .NET, so `pwsh Build-Release.ps1`, .NET restore/build/pack, PDFsharp execution, macOS signing, Homebrew provisioning, and Apple notarization are not claimed here. The Mac/Windows release hosts remain the authoritative execution tests.
