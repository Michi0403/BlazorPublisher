# PublisherStudio 3.2.6 source validation

Static/source validation only; no .NET restore, build, publish, DocFX render, GitHub access, or macOS native packaging tools were executed in this environment.

- Confirmed PublisherStudio application and installer-console versions are 3.2.6 and npm package metadata/cache-busters match.
- Confirmed the macOS package lane removes non-target Apple runtime folders, inventories every Mach-O file, writes `Contents/Resources/native-architecture-manifest.txt`, and reports exact incompatible relative paths.
- Confirmed the macOS launcher distinguishes physical Apple-Silicon capability, process architecture, and Rosetta translation state instead of treating `uname -m` as physical hardware identity.
- Confirmed macOS Info.plist generation includes explicit `LSArchitecturePriority`; ARM64 bundles additionally request native execution.
- Confirmed the working 3.2.5 fixed-port PublisherStudio startup behavior, visible Terminal console, user-data write checks, release staging cleanup, headless DMG, and PKG validation remain present.
- Confirmed README/LICENSE/THIRD-PARTY-NOTICES describe the Future2 role, Apache-2.0 project ownership, current NuGet/npm restore path, and separate DevExpress licensing boundary.
- Confirmed version-bearing XML/JSON files parse and the version-specific 3.2.6 source audit passes.
- The supplied source ZIP does not contain the `tests/` files referenced by package scripts/architecture guidance, so those repository tests could not be executed here and are not claimed as passed.
- Confirmed no repository-local `bin` or `obj` directory is included in the delivered source ZIP.
