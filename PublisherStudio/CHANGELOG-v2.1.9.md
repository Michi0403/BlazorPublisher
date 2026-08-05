# PublisherStudio 2.1.9

## Release, installer, and Pages completion

- Places the GitHub Pages workflow, validator, and pinned documentation snapshot at the repository root so GitHub Actions can discover and run them.
- Updates the snapshot refresh command to write the repository-root Pages asset and validates it before replacement.
- Aligns every active PublisherStudio version surface, setup banner, npm manifest, runtime metadata, documentation source, and tracked Pages snapshot to 2.1.9.
- Replaces the fragile release `Compress-Archive` calls with a deterministic .NET ZIP writer that snapshots files, retries transient reads, writes through a temporary archive, and verifies the wrapper layout.
- Keeps `NuGet.Config` and the empty protocol package-cache directory portable on case-sensitive source and CI environments.
- Adds regression coverage for repository-root Pages discovery, snapshot refresh routing, version alignment, and release archive creation.
- Repairs the publish and installer build guards so they require the verified ZIP writer instead of still demanding the removed `Compress-Archive` calls.
- Makes the Pages workflow reject a snapshot whose version, PDF payload, or local project-page links do not match the current PublisherStudio source.
- Replaces stale repository-root release instructions, including the old wire-package version, with one current authoritative entry point.
- Removes the leaked generated `.print-book` workspace and permanently excludes hidden DocFX tool/print work directories from source packages.

## Preserved release behavior

- Development exception diagnostics remain bounded to development builds and keep release logging unchanged.

- The first-chance exception observer remains development-only.
- No application static state was introduced; maintained DI and host-boundary rules remain enforced.
