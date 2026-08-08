# PublisherStudio 2.2.10 — console release wiring

- Keeps the working 2.2.9 PublisherStudio application, Kawaii DocFX site, API reference, PDF viewer, and user-supplied `NormalizeUrl` char-overload fix.
- Corrects the `.github/pages` MSBuild path without an extra directory separator.
- Prevents the Pages seed target from running while the release script intentionally performs an assembly-only build with documentation disabled.
- The release script now generates/validates DocFX and PDF first, then explicitly seeds the matching GitHub Pages ZIP exactly once.
- Authoritative release builds clear repository-local `bin`/`obj` state before restore/build, reducing stale Visual Studio/MSBuild design-time state without touching NuGet caches or maintained assets.
