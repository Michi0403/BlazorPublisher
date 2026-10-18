# PublisherStudio 4.2.0 — maintenance and publishing migration

**Baseline:** the user-supplied complete **PublisherStudio 4.1.9** source archive. This patch is deliberately not based on or merged from a PublisherStudio 4.0.3 product tree. **Reference architecture:** LocalGPT 6.0.2.

## PowerShell-only maintained build architecture

- Removed the 132 Python files previously used for historical audits, active code/architecture gates, XML documentation maintenance and GitHub Pages validation, plus `build/PythonRuntime.Common.ps1`.
- Retained and converted the active architecture, service/method resilience, async continuation, async-only UI, iterator, Razor/DevExpress, structural-renderer, component lifecycle, transient state, PanelStudio persistence, cross-platform, documentation coverage and Pages entrypoints to PowerShell. Historical version-specific audit diaries are documented in `docs/maintenance/powershell-migration-history.md`, not invoked in current builds.
- Added `Assert-MaintenanceArchitecture.ps1` as an early fail-closed repository script/file/parser and Python-dependency inventory. It is wired into the ordinary non-design-time MSBuild path as well as the source preflight.
- Preserved the stronger PublisherStudio 4.1.9 DevExpress requirements, including structural-renderer `RAZORUI0015`, FormLayout and nested template context, popup viewport/overflow, geometry/containment, the native editor and native disclosure prohibitions and special geometry-sensitive renderers. The native Razor audit was extended rather than using the narrow 4.0.3 migration scan.
- GitHub Pages artifact validation, extraction and snapshot preparation now use PowerShell; the GitHub Actions workflow invokes that implementation. WSL's core prerequisite check no longer requires Python.

## One-command Debug and Release

- `Build-LocalDevelopment.ps1` is retained from 4.1.9, including its own dependency preparation and non-design-time DevExpress browser-asset ensure-current MSBuild logic.
- `Build-Release.ps1` remains the release coordinator and retains the 4.1.9 repository-local build storage, documentation cache and release matrix. It now requests the LocalGPT 6.0.2 packaging tool package version **1.1.2**.
- `Ensure-ReleasePackagingPackage.ps1` automatically resolves and validates the managed LocalGPT packaging tool, using a cached/sibling source package or the configured release package source, without `dotnet tool install`. Missing dependencies fail with a specific explanation rather than bypassing license/packaging requirements.
- Documentation's own MSBuild entrypoint automatically resolves that helper when a complete PDF is required and it was not supplied. PDF merge/optimization and native packaging invoke its verified DLL with `dotnet`; PublisherStudio does not vendor a competing PDF merger.
- The 4.1.9 documentation layout, persistent browser chunks, repository build-storage and Docs/Pages semantics remain intact.

## Product preservation and limitations

The C# and Razor implementation, including **PanelStudioTextService**, every UI component, persistence and authoring service, static asset, SQL file and vendored dependency is byte-for-byte unchanged. Only the web and installer project `<Version>` tags change from 4.1.9 to 4.2.0. No product methods were removed.

**Validation is source-level only.** PowerShell host validation, .NET restore/build, browser DocFX/PDF rendering, DevExpress runtime UI and Apple/Windows signing or native publishing were not executable in the packaging environment. The maintained native PowerShell scans are repository heuristics and cannot independently certify full Roslyn/parser behavioral equivalence to every removed historical Python analyzer; the compiler and the real Debug and Release commands remain required acceptance tests.
