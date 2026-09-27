# PublisherStudio 3.8.1 source validation

Source-only validation performed in the handoff environment:

- `build/read-devexpress-lock-metadata.cjs` passes Node syntax validation and successfully reads the supplied npm v3 lock file, reporting `devextreme-dist` 25.2.10 and `devexpress-aspnetcore-spreadsheet` 25.2.10.
- Maintained browser JavaScript passes Node syntax validation for the changed files, and the JavaScript diagnostics SHA-256 manifest was refreshed after the changes.
- DevExpress asset markup loads `dx.all.js` before `devextreme-license.js`; both remain pinned to 25.2.10.
- DataVisualEditor contains DevExpress interactive controls rather than native button/input/select/option/textarea controls.
- DevExpress component-retention manifest and guard are present and wired into `Directory.Build.targets`.
- Localization catalogs parse successfully and retain key parity; the German identical-value review baseline prevents newly introduced untranslated German UI values from silently passing.
- PublisherStudio.Web and PublisherStudio.InstallerConsole versions are 3.8.1; npm root package metadata is 3.8.1 while DevExtreme dependencies remain 25.2.10.
- No dotnet/MSBuild/NuGet build, restore, publish, installer execution, or GitHub access was performed.
