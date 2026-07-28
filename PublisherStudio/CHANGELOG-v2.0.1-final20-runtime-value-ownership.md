# PublisherStudio 2.0.1 final20 — runtime-value ownership repair

## Breaker fixed

- Removed the panel service-owned shutdown, HTML-break, HTML-tag, and unsafe-file-name regex fields.
- Added `IPanelStudioTextPatternDataService` and `PanelStudioTextPatternDataService`.
- Moved pattern text, regex options, and match timeouts into the serializable `Configuration/panel-text-patterns.json` store.
- Added a configured user-local override path without moving fallback values into components, controllers, or business services.
- Registered both the data service and panel text service through the central service-collection extension.
- Required configuration fails closed when missing or invalid.

## Architecture and security safeguards

- Added a removal-only runtime-value ownership baseline and PowerShell guard.
- Added a final19 1-Wire/security preservation hash manifest and guard.
- Added a final20 protected architecture-file manifest covering the safeguards, data boundary, object store, DI wiring, build entry points, and contract test.
- Wired protected-file, security-preservation, and runtime-ownership guards into local/release PowerShell builds and direct MSBuild builds after the existing 1-Wire check.
- Kept the reviewed final19 organic runtime security and 1-Wire implementation files unchanged.
- Kept `.sha256` safeguard manifests explicitly visible through `.gitignore` rules without running Git.

## Validation status

- Node final20 architecture/security contract test: passed.
- JSON/XML parsing and source-structure checks: passed.
- Security-rule hash verification against final19: passed.
- Native .NET compilation was not run because the supplied environment has no .NET SDK.
