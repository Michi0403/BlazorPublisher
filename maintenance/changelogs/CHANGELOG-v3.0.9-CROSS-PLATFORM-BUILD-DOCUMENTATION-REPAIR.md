# PublisherStudio 3.0.9 — Cross-Platform Build and Documentation Runtime Repair

## Fixed

- Removed Windows-only gating from active MSBuild repository guards. `Directory.Build.targets` now selects Windows PowerShell on Windows and `pwsh` on macOS/Linux, while keeping every existing guard enabled.
- Restored the second reviewed `NativeDeviceDiscovery` catch boundary as an explicit cancellation boundary and restored the required logging-integrity policy document. The committed logging baseline was not lowered or weakened.
- Corrected `Assert-ServiceArchitecture.ps1` to validate the existing tokenized `('--product', 'publisherstudio')` service-audit invocation instead of incorrectly requiring the two tokens as one literal string.
- Changed project builds so Debug continues to generate the complete HTML help site but does not force the heavyweight PDF. Release builds and `Build-Release.ps1` still require the complete versioned PDF once.
- Kept the preferred browser-PDF lane cross-platform by probing the standard Linux Edge/Chrome command names as well as the existing Windows paths and macOS application bundles.
- Reused any already-installed Node.js 20+ runtime for documentation, DevExtreme, and Spreadsheet asset preparation instead of provisioning a second Node.js copy only because the installed major is newer than the preferred LTS major.
- Made redirected DocFX/Playwright progress compact and platform-neutral: carriage-return redraws no longer flood macOS/Linux terminals, mojibake block bars are not printed on Windows/MSBuild, and repeated unchanged PDF percentages are de-duplicated. Diagnostics and failure text remain captured and visible.

## Preserved

- The 3.0.8 macOS browser discovery repair and fast single-browser PDF path.
- Complete documentation content, accessibility/link validation, PDF requirement for release packaging, application behavior, UI, InteractiveServer boundaries, persistence, DevExpress/Spreadsheet functionality, publication formats, and LocalGPT protocol integration.
- No build guard was disabled, bypassed, or weakened.
