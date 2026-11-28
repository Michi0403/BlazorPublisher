# PublisherStudio 2.3.2 — frontend integrity refresh

- Adds a canonical PowerShell manifest generator for the existing JavaScript diagnostics SHA-256 inventory.
- Ordered development, release and all-runtime build scripts refresh then validate the inventory before restore/build.
- Direct Visual Studio/MSBuild builds remain validation-only so an unreviewed JavaScript edit is not silently blessed.
- Adds `Update-FrontendIntegrity.cmd` for explicit manual refresh + validation after frontend work.
