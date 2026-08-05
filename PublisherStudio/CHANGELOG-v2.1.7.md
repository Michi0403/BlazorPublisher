# PublisherStudio 2.1.7

## Symmetric Kawaii documentation shell

- The left table-of-contents rail and right in-article rail now use the same responsive width.
- Both rail-to-article gaps use one shared spacing variable.
- The centered shell can grow to 112rem, and the article consumes the complete remaining desktop width instead of stopping at a fixed character width.
- Short pages fill one viewport; normal document scrolling begins only when real content exceeds it. Side rails do not create nested scroll areas.
- The pinned dependency-free Pages snapshot now carries the same three-column geometry and a real left documentation rail.
- The layout contract is intentionally shared with LocalGPT to keep both product documentation sites visually predictable.
- Mobile and tablet DocFX behavior remains unchanged.

## Development exception diagnostics retained

- The development-only, DI-owned first-chance exception observer remains enabled.
- Expected cancellation, disposal, disconnected-circuit, and framework lifecycle exceptions remain classified at Debug level.
- Unexpected PublisherStudio exceptions retain contextual Warning logging and bounded repeat summaries.
- Host shutdown and runtime-endpoint cleanup paths retain explicit logs.
- No application static state or static convenience logger was introduced.

## Windows release-build and DocFX link correction

- The documentation payload assertion now uses the comparison-aware `String.IndexOf` overload supported by Windows PowerShell 5.1 instead of the PowerShell 7-only two-argument `String.Contains` overload.
- The Kawaii documentation home cards now link to maintained Markdown inputs, allowing DocFX to resolve and rewrite them without `InvalidFileLink` warnings.
- The publish-configuration guard rejects future comparison-aware `String.Contains` calls in `Build-Release.ps1` so the all-runtime release lane remains compatible with the `powershell.exe` host used by MSBuild.

