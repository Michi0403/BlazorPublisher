# PublisherStudio 3.2.6 — macOS architecture diagnostics, Future2 positioning, and license clarification

- Preserves the 3.2.5 PublisherStudio application behavior that was confirmed working after PKG installation.
- Adds package-time removal of non-target Apple runtime asset folders before final macOS architecture validation.
- Inventories every Mach-O file in the finished `.app` and writes the exact `file` result to `Contents/Resources/native-architecture-manifest.txt`. Any incompatible native component stops packaging with its exact relative filename instead of producing a vague Intel/ARM warning later.
- Makes launcher architecture checks Rosetta-aware by deriving physical Apple-Silicon capability from macOS `sysctl` rather than treating `uname -m` as hardware identity; translated Apple-Silicon launches can re-exec through the native ARM64 system shell.
- Adds explicit LaunchServices architecture priority metadata; ARM64 bundles request native execution.
- Reworks the README around PublisherStudio's stable product purpose instead of a version-specific release paragraph and documents its standalone creative/productivity role in the Future2 direction.
- Replaces Windows-only-looking top-level build steps with the maintained cross-platform PowerShell entry points while retaining `.cmd` wrappers for Windows.
- Clarifies the DevExpress boundary: PublisherStudio's own source remains Apache-2.0; DevExpress remains proprietary. Current .NET package restore uses NuGet.org, npm assets follow `package-lock.json`, and the private DevExpress developer license is a separate build-time identity that is not included in end-user installations.
- Preserves user-data permission checks, visible macOS console, local startup behavior, Pages/DocFX behavior, staging cleanup, headless DMG generation, validated PKG layout, and existing InteractiveServer boundaries.
- Version advanced from 3.2.5 to 3.2.6.
