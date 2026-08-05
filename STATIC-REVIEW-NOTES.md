# Static review notes

This source package is based on `PublisherStudioCleanedByCorruption.zip`.

Restored/reworked:

- Serializable PublisherStudio DX function catalog and object-store-backed capability provider.
- DI registration for the catalog and shared 1-Wire capability-provider contract.
- Recoverable user-local catalog overrides with deployed-seed fallback.
- Explicit build behavior with no automatic repository-policy scripts in `Directory.Build.targets`.
- Source package cleanup: IDE state, user files, logs, and build output are excluded.

Validation limitation: reviewed statically only. No .NET SDK/runtime or PowerShell execution was available or used. XML/JSON structure, source references, archive paths, and source-level syntax heuristics were checked before packaging.
