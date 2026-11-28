# PublisherStudio 2.2.9 source validation

- Keeps the 2.2.8 API Pages/accessibility and automatic snapshot seeding repair baseline.
- Fixes the same MSBuild repository-relative snapshot output path issue found by the LocalGPT real build, preventing `.github` from being concatenated as `BlazorPublisher.github`.
- Keeps the full-width documentation viewer, generated API reference, PDF generation, service resilience, and the `NormalizeUrl` char-overload compiler fix unchanged.
- The tracked older Kawaii snapshot is intentionally not relabeled; the first successful 2.2.9 Debug/Release build will generate and seed the real versioned snapshot.
- Source package contains no build output directories or compiled DLL/EXE/PDB artifacts.
