# PublisherStudio 2.8.3 source validation

This package is intentionally source-only. No `dotnet`, MSBuild, restore, build, publish, pack, or GitHub/network repository command was run while preparing it.

## Passed source checks

- Mirrored the exact `Assert-TextServiceOwnership.ps1` matching/baseline policy against Components/Controllers: passed with no new findings.
- `build/audit_release_2_8_3.py`: 48 checks passed.
- Retained `build/audit_release_2_8_2.py`: 167 checks passed after rolling only current-version/cache assertions.
- Retained `build/audit_release_2_8_1.py`: 62 checks passed after rolling only current-version/cache assertions.
- Application architecture audit: passed.
- Service resilience audit: 1,293 service methods with try/catch + diagnostics; 4 iterator/yield methods with try/finally + diagnostics; 4 direct Program/Startup methods skipped by policy.
- Async continuation audit: 75 source files; 1,058 await tokens; passed.
- Panel Studio persistence audit: passed.
- Documentation / 1-Wire contract audit: passed.
- XML documentation coverage: 5,529 direct C# declarations across 190 maintained C# files; passed.
- Project XML and `appsettings.json` parsing: passed.
- Reviewed InteractiveServer boundary set remains exactly five files.
- LocalGPT 1-Wire protocol package version remains 2.1.1.

## Scope of the repair

The 2.8.3 repair is deliberately narrow. `PageSurface.razor` no longer performs the new `string.Join` operation that violated PublisherStudio's text-service ownership policy. Deterministic selection-key construction now belongs to the injected singleton `PublicationEditorTextService`, with exception diagnostics and a safe fallback.

No z-index redesign, Video Studio layer/effect replacement, media-quality policy change, EF migration, or 1-Wire version change was introduced.
