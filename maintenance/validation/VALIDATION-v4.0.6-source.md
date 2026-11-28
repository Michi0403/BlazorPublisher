# PublisherStudio 4.0.6 source validation

## Scope

Focused code-side maintenance repair for the reported `Assert-TextServiceOwnership.ps1` failure in `PanelStudio`. The maintenance guard is preserved unchanged.

## Confirmed source checks

- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.0.6**.
- `PanelStudio.razor` no longer calls `string.Join` directly for Data Visual value fields.
- `PanelStudioTextService.FormatList` owns the list formatting operation and includes method-local catch/log diagnostics.
- Existing `PanelStudioTextService.ParseList` remains the edit/parse owner, so formatting and parsing stay within the same service boundary.
- The publication model, saved document format, Panel Studio structural-renderer roots and DevExpress-first UI ownership remain unchanged.

## Source validation executed

The following repository-maintained source audits passed on this source tree:

- application architecture / static policy / C# structure;
- async continuation ownership;
- async-only component/service architecture;
- component method resilience;
- Razor maintenance architecture;
- prerender JavaScript interop safety;
- cross-platform boundaries; and
- Panel Studio persistence.

The maintained PowerShell text-service ownership rule was also reproduced source-for-source in Python because PowerShell is unavailable in this preparation environment; it reports no new direct component/controller string/regex operations.

The repository XML documentation audit passes for 6,417 direct C# declarations across 260 maintained source files and 3,845 direct Razor `@code` member declarations across 49 components, including the new `PanelStudioTextService.FormatList` method.

## Packaging and maintenance integrity

- No file under `build/` is changed relative to the supplied 4.0.5 source package.
- Historical changelogs and validation files are retained unchanged.

## Environment limitation

The .NET SDK/MSBuild, PowerShell and GitHub are not used in this preparation environment. No restore/build/publish or runtime UI execution is claimed. The maintainer's .NET 10 + DevExpress build remains authoritative for compiler/runtime verification.
