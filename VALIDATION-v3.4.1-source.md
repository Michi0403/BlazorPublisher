# PublisherStudio 3.4.1 source validation

This source package was validated statically without running a .NET build and without GitHub access.

## PowerShell compatibility checks

- Scanned every maintained `.ps1`/`.psm1` for the compatibility patterns enforced by `build/Assert-PowerShellCompatibility.ps1`.
- Confirmed there is no direct `String.Contains(value, StringComparison)` call.
- Confirmed there is no direct `System.IO.Path.GetRelativePath(...)` call.
- Confirmed there is no direct `ProcessStartInfo.ArgumentList` access.
- Confirmed there is no direct `Process.Kill(true)` call.
- Confirmed portable reflection/fallback helpers are present.

## Release preservation

- The 3.4.0 release orchestration, chunked PDF cache/resume, notarization recovery, and application behavior were retained.
- Current version references were advanced to 3.4.1 while historical changelog/validation files remain intact.
- No .NET build was performed in this environment.
