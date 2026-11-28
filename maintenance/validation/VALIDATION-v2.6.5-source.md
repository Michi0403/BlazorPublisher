# PublisherStudio 2.6.5 source validation

This release was validated without invoking dotnet, MSBuild, GitHub, or a browser automation build. The user's Windows build remains authoritative.

## Completed source checks

- PublisherStudio architecture audit: passed.
- Service resilience audit: passed for 1,277 service methods; 4 iterator/yield methods and 4 direct Program/Startup methods remain intentional exclusions.
- Panel Studio persistence regression audit: passed.
- 2.6.0 typed-data/panel/media regression audit: passed.
- 2.6.2 Picture Studio/page-effects regression audit: passed.
- 2.6.5 preview/AI/export compatibility audit: passed.
- 2.6.5 media-studio/drag/effect/localization audit: passed.
- Documentation/1-Wire contract audit: passed.
- XML documentation coverage: 5,497 direct C# declarations across 183 maintained source files; enrichment second pass made 0 changes.
- Node syntax validation: all 16 maintained PublisherStudio browser JavaScript files passed.
- JavaScript diagnostics manifest: regenerated from normalized LF content for all 16 maintained browser files.
- Localization: en-US/de-DE/es-ES/ja-JP contain 3,251 matching, case-insensitively unique keys.
- Current Mainframe/Picture/Video `LT(...)` surface: 1,197 canonical literals are catalogued. German values identical to English are limited to 82 primarily numeric, product, format or internationally shared technical labels.
- Project/build XML, appsettings JSON and all localization JSON parse successfully.
- Modified Razor files preserve their existing `@rendermode` directives compared with 2.6.4.
- Extracted standalone 3D browser-runtime JavaScript passes Node syntax validation.

## Version/format

- PublisherStudio.Web: 2.6.5.
- PublisherStudio.InstallerConsole: 2.6.5.
- Publication format: 1.58 unchanged.
- Picture Studio document format: 1.5 unchanged.
- No database migration.
