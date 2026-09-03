# PublisherStudio 3.2.7 source validation

Static validation performed without invoking the .NET SDK/build:

- Reproduced the 3.2.6 release failure through the same Python service-resilience audit called by `build/Assert-MethodDiagnostics.ps1`: `ApplicationPathService.KnownFolderOrFallback` was the single failing service method.
- Repaired that method with an owned `try/catch` and structured `ILogger` diagnostics while preserving its known-folder/per-user-fallback behavior.
- Re-ran `audit_application_architecture.py --mode methods`: PASS.
- Re-ran `audit_service_resilience.py`: PASS for all maintained PublisherStudio service methods.
- Confirmed application/installer/npm/docs/cache-buster version metadata is 3.2.7.
- Confirmed the four reviewed `@rendermode InteractiveServer` declarations remain present and unchanged in count.
- Confirmed the 3.2.6 macOS architecture hardening markers remain in `build/NativeReleasePackaging.ps1`.
- Confirmed version-bearing XML/JSON parse successfully and the 3.2.7 source audit passes.
- Confirmed no repository-local `bin` or `obj` directories are included in the delivered source ZIP.

No `dotnet restore`, `dotnet build`, `dotnet publish`, or GitHub access was used for this repair.
