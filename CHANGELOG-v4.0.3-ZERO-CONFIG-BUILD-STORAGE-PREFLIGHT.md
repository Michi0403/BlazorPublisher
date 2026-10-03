# PublisherStudio 4.0.3 — zero-configuration build-storage preflight repair

## Fixed

- Fixed `Assert-PowerShellCompatibility.ps1` failing under `Set-StrictMode` because a source-code search used a double-quoted string containing `$documentationToolCacheRoot`. PowerShell attempted to resolve that name as a variable in the maintenance script instead of searching for the literal source text.
- A normal clone now has an explicitly guarded zero-configuration storage contract: when no override is supplied, heavy build state defaults to `<repository>/artifacts/.build-storage`. `FUTURE2_BUILD_STORAGE_ROOT` and `-DocumentationCacheRoot` remain optional overrides only.
- `.gitignore` now documents `artifacts/.build-storage/` explicitly in addition to the existing `artifacts/` rule so developers can see where local release caches belong without reading the build scripts.

## Maintenance protection

- `Assert-PowerShellCompatibility.ps1` verifies the repository-local default and `.gitignore` ownership contract.
- `build/audit_repository_build_storage.py` now rejects the exact double-quoted `$documentationToolCacheRoot` source-search regression as well as loss of the zero-configuration default or ignore rule.

## Runtime scope

No PublisherStudio application/editor runtime behavior changed. This release repairs release-build bootstrap/preflight behavior only.
