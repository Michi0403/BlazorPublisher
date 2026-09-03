# PublisherStudio 3.2.8 source validation

Static validation performed without invoking the .NET SDK/build:

- Confirmed the reported 3.2.7 failure path: durable DocFX HTML was reused, the tool restore was skipped, Microsoft Edge printing failed, and the DocFX plug-in fallback attempted to invoke an unresolved command target.
- Added `Ensure-PublisherStudioDocfxToolForPdfFallback` so the repository-local DocFX command is restored lazily and the pinned isolated DocFX 2.78.5 tool path remains available as a secondary fallback.
- Confirmed the PDF fallback explicitly refuses to invoke DocFX unless a runnable command target has been resolved.
- Lowered the browser-print source-page limit to 600 so the observed 732-page PublisherStudio documentation uses the DocFX PDF plug-in directly.
- Re-ran the maintained Python application-architecture and service-resilience audits: PASS.
- Confirmed application/installer/npm/docs/cache-buster version metadata is 3.2.8.
- Confirmed the four reviewed `@rendermode InteractiveServer` declarations remain present and unchanged in count.
- Confirmed the macOS architecture hardening markers remain in `build/NativeReleasePackaging.ps1`.
- Confirmed version-bearing XML/JSON parse successfully and the 3.2.8 source audit passes.
- Confirmed no repository-local `bin` or `obj` directories are included in the delivered source ZIP.

No `dotnet restore`, `dotnet build`, `dotnet publish`, or GitHub access was used for this repair.
