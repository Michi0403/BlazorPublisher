# PublisherStudio 3.4.1 - PowerShell runtime compatibility

## Fixed

- Replaced the PDF cache completeness check's `String.Contains(value, StringComparison)` call with the Windows PowerShell 5.1-compatible `String.IndexOf(value, comparison)` form.
- Removed direct `System.IO.Path.GetRelativePath` usage from native packaging and use the shared portable reflection/URI approach.
- Reworked browser process argument setup so modern pwsh can use `ArgumentList` through reflection while Windows PowerShell 5.1 uses a safely quoted `Arguments` fallback.
- Reworked process-tree termination so modern runtimes use `Kill(true)` through reflection and Windows PowerShell 5.1 falls back to `Kill()`.
- Extended the compatibility preflight to reject these API families before expensive release work starts.

## Preserved

- 3.4.0 orchestration resilience, chunked Edge rendering, durable PDF chunk reuse, compression, notarization recovery/resume, signing, packaging, and InteractiveServer behavior remain unchanged.

## Version

- PublisherStudio advanced from 3.4.0 to 3.4.1. Minor and patch slots remain single digit.
