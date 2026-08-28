# PublisherStudio 3.0.9

PublisherStudio 3.0.9 is the **Cross-Platform Build and Documentation Runtime Repair** release.

This patch repairs the exact 3.0.8 build-guard and documentation-tooling failures. Repository guards remain enabled and now execute on Windows, macOS, and Linux through the appropriate PowerShell host. The delegated native-device facade again owns the reviewed cancellation and failure catch boundaries, its required logging-integrity policy document is restored, and the service-architecture guard validates the existing tokenized `--product publisherstudio` invocation correctly. Debug builds keep generated HTML help but do not force the heavyweight PDF; the release path still requires the complete versioned PDF once. Existing Node.js 20+ installations are reused instead of provisioning a second runtime, and redirected DocFX progress is compact and de-duplicated across platforms.

Application features, UI behavior, InteractiveServer boundaries, persistence, DevExpress/Spreadsheet functionality, publication formats, LocalGPT wire protocol integration, and release packaging contracts are unchanged. This handoff is source-only; no .NET build and no GitHub access were used while preparing it. See `CHANGELOG-v3.0.9-CROSS-PLATFORM-BUILD-DOCUMENTATION-REPAIR.md` and `VALIDATION-v3.0.9-source.md`.
