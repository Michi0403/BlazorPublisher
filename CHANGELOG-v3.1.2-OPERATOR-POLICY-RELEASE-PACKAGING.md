# PublisherStudio 3.1.2 — operator policy and release packaging

## Changed

- Increased PublisherStudio Web and installer-console versions from 3.1.1 to 3.1.2. The release numbering rule keeps every numeric slot at one digit; e.g. x.y.10 rolls to x.(y+1).0.
- Reworked operator-owned runtime maxima so shipped limits are permissive where the local application can safely let the operator decide.
- Removed the remaining `MaximumTrackedMessages = 4096` Organic/1-Wire replay ceiling. Replay tracked-message capacity is now read from persisted system-variable policy and ships at `Int32.MaxValue`.
- Retained replay timestamp/skew and cleanup behavior as protocol/security mechanics rather than silently expanding the accepted security window.
- Preserved PublisherStudio's existing application/service architecture instead of introducing static runtime-limit helpers.

## Release packaging

- Added the repository-owned `LocalGPT.ReleasePackaging` .NET tool/package source and package resolver/cache workflow.
- TAR.GZ, DEB, and SHA-256 package work is routed through the repository-owned packaging tool; `dpkg-deb` is not required.
- Windows remains the only setup-console publishing path (`win-x64`, `win-x86`, `win-arm64`).
- macOS (`osx-x64`, `osx-arm64`) builds Full + Light application payloads with `.tar.gz` and `.dmg` output; branded DMG creation remains a native macOS `hdiutil` operation.
- Linux (`linux-x64`, `linux-arm64`) builds Full + Light `.tar.gz`, `.deb`, `.rpm`, and `.AppImage`; RPM/AppImage prefer native tooling and can use Docker/Podman fallback.
- PublisherStudio keeps the FFmpeg licensing boundary: native package-manager packages request FFmpeg from the system package manager while portable packages retain the dependency helper path.
- Versioned release output retains SHA-256 checksum generation.

## UI / rendering safety

- The Interactive Server page boundaries from the supplied 3.1.0 source baseline are preserved; none of those page directives were removed.
- Existing prerender/JavaScript interop, component resilience, and Panel Studio persistence gates remain part of validation.

## Validation boundary

This handoff is source-only by request. No GitHub access and no .NET build are used as release evidence. The maintained Python source audits are run on a fresh extraction of the exact delivered ZIP before handoff; see `VALIDATION-v3.1.2-source.md`.
