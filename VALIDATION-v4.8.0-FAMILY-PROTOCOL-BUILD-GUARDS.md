# PublisherStudio 4.8.0 — family JSON build-guard validation

## Observed 4.7.9 failure

The actual Windows Debug build rejected the new `Services/OrganicPlugins/FamilyWireJsonConventions.cs` at three independent gates:
1. `Assert-LoggingIntegrity.ps1`: missing structured logger and catch/log boundary for a new operational Service.
2. `Assert-ServiceArchitecture.ps1`: static class not an approved Service helper.
3. `Assert-ApplicationStaticPolicy.ps1`: public static application class outside the approved composition-root boundary.

## Correction and source-level expectations

- `BusinessObjects/FamilyWireJsonConventions.cs` is a non-static, stateless configuration type outside operational Service policy; its instance methods do no I/O and have no mutable shared state.
- `OrganicPluginProtocolCodec` and MVC program configuration use the same convention, preserving string-enum and numeric-string read behavior.
- The existing three architecture guards are **unchanged**. `Assert-FamilyProtocolCompatibility.ps1` now rejects reintroduction of the old Service type or a static conventions class.
- The installer scripts and all unrelated source files are unchanged.

## Run on the user's Windows build host

```powershell
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File ./build/Assert-FamilyProtocolCompatibility.ps1
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File ./build/Assert-LoggingIntegrity.ps1
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File ./build/Assert-ServiceArchitecture.ps1
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File ./build/Assert-ApplicationStaticPolicy.ps1
```

Run the normal PublisherStudio Debug build and then the established Release builds for Windows, macOS and Linux. Validate a LocalGPT 7.1.5 / PublisherStudio 4.8.0 1-Wire Council request and the existing signed/approved response on each platform.

**Limitations:** source-level/static comparisons only in the preparation environment. No PowerShell interpreter, .NET SDK, licensed DevExpress restore, or live app-to-app test was available here. Do not mark runtime behavior verified until those execute successfully.
