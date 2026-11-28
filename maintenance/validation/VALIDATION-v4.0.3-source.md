# PublisherStudio 4.0.3 source validation

## Scope

Build-storage bootstrap/preflight repair only. Runtime/editor behavior is unchanged from 4.0.2.

## Confirmed source checks

- Repository build storage defaults to `<repository>/artifacts/.build-storage` without requiring environment configuration.
- `.gitignore` explicitly ignores `artifacts/.build-storage/` and the parent `artifacts/` output tree.
- The PowerShell compatibility guard searches `$documentationToolCacheRoot` as literal source text and therefore does not resolve an unset maintenance-script variable under StrictMode.
- `build/audit_repository_build_storage.py` checks all three properties above and passes.
- Active PublisherStudio Web, InstallerConsole, npm package/lock, and browser cache-buster identities are aligned at **4.0.3**.

## Environment limitation

`pwsh` and the .NET SDK are not used in this preparation environment. No PowerShell runtime build, dotnet restore/build/publish, documentation render, notarization, or native package build is claimed.
