# PublisherStudio 3.0.2 - Cross-platform Python runtime resolution

PublisherStudio 3.0.2 fixes the release/audit pipeline on macOS and Linux hosts where Python 3 is installed as `python3` rather than `python`.

## Build tooling repair

- Added `build/PythonRuntime.Common.ps1` as the shared Python 3 resolver for PublisherStudio build guards.
- Resolution order is `python`, `python3`, then the Windows `py -3` launcher.
- `Assert-SourcePackagePrerequisites.ps1` now verifies Python 3 before the ordered build and reports the exact runtime selected.
- Panel Studio persistence and XML-documentation audits no longer invoke a hard-coded `python` executable.
- Architecture, iterator, async-continuation, service-resilience and component/prerender audits use the same host-neutral resolver.
- The cross-platform source audit now rejects a return to bare `& python` calls in maintained build guards and verifies the shared resolver is wired into the release preflight.
- Completed XML documentation for the new 3.0.1 platform-boundary declarations and the maintained Razor members already enforced by the release XML-documentation gate, so the build does not merely move from the Python failure to the next preflight failure.

## Scope

The 3.0.1 backend platform interfaces, Windows/Unix DI boundaries, GDI/System.Drawing cleanup, DocFX accessibility flow and GitHub Pages preparation remain unchanged. No GitHub remote state is modified by these source changes.
