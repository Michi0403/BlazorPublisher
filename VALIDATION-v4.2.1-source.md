# PublisherStudio 4.2.1 — source validation

## Source integrity

- Baseline: `PublisherStudio-4.2.0-source.zip` (flat-root), 1868 source files.
- Preserved original paths: 1868 / 1868; no removed baseline files.
- Modified original files: 14, of which the only application project changes are version-only `.csproj` edits.
- Added files: 4 (new guard + manifest + changelog + this report).
- Removed Python maintenance files in *new* ZIP: none shipped; 132 exact Python script paths and one obsolete PowerShell resolver are migration-only data, not executable Python.
- Original application implementations, Razor, JS, CSS, SQL, and vendored/license contents: byte-for-byte preserved.

## Static checks executed

- Verified the five failure sites and their replacement contracts.
- Simulated maintained method-body offset and exception/log policy on 22 methods; all passed.
- Verified `Editor.razor` lifecycle separation (`OnInitialized` vs `OnAfterRenderAsync`).
- Verified all PowerShell `RepositoryRoot` parameter-default early-binding patterns were removed across build scripts.
- Verified Python removal inventory paths and 64-hex SHA-256 references.
- Parsed project and MSBuild XML and retained the original guard wiring.
- Verified all 4.2.0 paths and unchanged bytes where required.
- Did **not** run PowerShell 5.1, .NET/Visual Studio compile, runtime UI, or native release in this environment.

Changed original paths:
- `build/Assert-ComponentSafety.ps1`
- `build/Assert-CrossPlatformBoundaries.ps1`
- `build/Assert-DevExpressComponentRetention.ps1`
- `build/Assert-MaintenanceArchitecture.ps1`
- `build/Assert-RazorComponentAttributeExpressions.ps1`
- `build/Assert-RazorMaintenanceArchitecture.ps1`
- `build/Assert-ServiceArchitecture.ps1`
- `build/Assert-ServiceMethodResilience.ps1`
- `build/Assert-SourcePackagePrerequisites.ps1`
- `build/Assert-TransientUiStateOwnership.ps1`
- `build/Assert-XmlDocumentationCoverage.ps1`
- `build/Invoke-ArchitectureAudit.ps1`
- `src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj`
- `src/PublisherStudio.Web/PublisherStudio.Web.csproj`

New source paths:
- `CHANGELOG-v4.2.1-MAINTENANCE-DEBUG-REPAIR.md`
- `VALIDATION-v4.2.1-source.md`
- `build/Migrate-ObsoletePythonMaintenance.ps1`
- `build/obsolete-python-maintenance.sha256`
- `VALIDATION-v4.2.1-source.md`
