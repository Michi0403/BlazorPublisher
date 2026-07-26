# PublisherStudio v1.0.86

## .NET SDK implicit-content compatibility

- Fixed `NETSDK1022` in `PublisherStudio.Web.csproj` for the four file-based localization resources.
- Kept the SDK Web project's default content discovery enabled and changed the project-local localization glob from `Content Include` to `Content Update`.
- Preserved `CopyToOutputDirectory="PreserveNewest"` and `CopyToPublishDirectory="PreserveNewest"`, so localization files still ship beside the application and in publish output.
- Did not use the broad `EnableDefaultContentItems=false` workaround, because that would require manually maintaining every web/content item and would make future feature additions easier to omit.
- Added a regression contract that fails if localization resources are explicitly re-included, if default content is globally disabled, or if any starter localization JSON file becomes missing or invalid.

## Compatibility and task status

- **Closed:** the .NET 10 SDK duplicate `Content` blocker reported after v1.0.85.
- **Closed and retained:** v1.0.85 interface-first services, controller APIs, OpenSCAD graph, browser automation, screenshot workflow, localization, configurable paths and render-export extensions remain unchanged.
- **Partial:** native .NET/Razor/DevExpress compilation can now continue past `NETSDK1022`; subsequent compiler or package findings depend on the licensed developer-machine build and should be handled in the next iteration without masking them.
- **Deferred:** all previously documented v1.0.85 deferred tasks remain in `docs/architecture/task-ledger.md` until explicitly closed by a later tested release, including the visual OpenSCAD builder, native OpenSCAD execution, operating-system-global input, full localization migration, and remaining static-to-service evolution.

The maintained task-status source remains `docs/architecture/task-ledger.md`.
