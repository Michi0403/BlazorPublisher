# PublisherStudio 3.0.1 source validation

This handoff is validated statically. No `dotnet` restore/build/test/publish and no PowerShell release build were executed in the packaging environment.

Validated source contracts:

- PublisherStudio Web and installer identities are `3.0.1`; browser cache identities and npm package identities match.
- `System.Drawing.Common` and the unused explicit `System.Security.Cryptography.ProtectedData` dependency are absent from the maintained web project.
- Maintained C#/Razor source is free of `System.Drawing`/GDI calls.
- Host decisions are confined to the composition root and platform-specific implementations; common services consume Windows/Unix-neutral interfaces.
- Windows and Unix implementations are registered for platform runtime, global hotkeys, process loopback and native device discovery.
- Filesystem containment checks in documentation and LAN services use the injected host path semantics.
- The authored DocFX source tree and cross-platform Node runtime helper are present in the source package.
- Documentation HTML is validated before PDF rendering; Pages validation distinguishes tagged PDFs from the DocFX HTML-accessibility fallback.
- Async continuation policy validates the maintained source with zero unconfigured awaits.
- InteractiveServer render-mode ownership is unchanged from the 3.0.0 source baseline.
- Source ZIP validation requires repository-root layout, duplicate/case/Unicode collision checks and `unzip -t` integrity verification.

Run the authoritative project build/release scripts on licensed Windows/macOS/Linux build hosts before publication.
