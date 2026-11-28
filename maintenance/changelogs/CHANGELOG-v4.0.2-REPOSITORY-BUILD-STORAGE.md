# PublisherStudio 4.0.2 — repository-local release-build storage

## Fixed

- Full release builds now keep heavy mutable tool state with the repository by default instead of allowing macOS/Linux per-user caches and the operating-system temp directory to consume the internal system partition while the checkout itself lives on an external APFS volume.
- `Build-Release.ps1` initializes `artifacts/.build-storage` before compile, restore, DevExpress preparation, documentation, browser rendering, and native packaging work. The build redirects `DOTNET_CLI_HOME`, NuGet package/HTTP/plugin/scratch caches, npm cache, XDG cache, documentation caches, and `TMPDIR`/`TMP`/`TEMP` into that repository-owned root.
- Standalone documentation builds initialize the same storage policy before DocFX, browser-PDF, and temporary profile work.
- Documentation/Node runtime cache resolution now prefers the supplied repository fallback before `LocalApplicationData`; the user profile is only a last-resort standalone fallback.

## Why this matters on macOS

A repository under `/Volumes/...` does not automatically move dotnet/NuGet, npm, DocFX, browser-profile, or OS-temp state off the internal disk. The previous release path could therefore keep `bin/obj/artifacts` external while still filling `~/Library`, `~/.nuget`, and `/private/var/folders` through tool defaults.

## Maintenance protection

- `Assert-PowerShellCompatibility.ps1` now verifies the repository build-storage helper, all redirected cache/temp environment variables, release-entrypoint ordering, standalone documentation initialization, and fallback-before-user-cache policy.
- `build/audit_repository_build_storage.py` mirrors the contract in source-only environments where PowerShell is unavailable.
- `AGENTS.md` documents the repair pattern for future build tools.

## Operator override

`FUTURE2_BUILD_STORAGE_ROOT` may point the heavy build state at another build volume. `-DocumentationCacheRoot` remains a narrower override for documentation state.
