# PublisherStudio 4.0.7 source validation

## Scope

Focused source-side maintenance repair for the reported DevExpress/Razor compiler failures in `DataManager`, `MediaConverterStudio`, `PanelStudio`, and `PageEffectStudio`. No maintenance guard is weakened or modified.

## Confirmed source checks

- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.0.7**.
- All five reported editable string `DxComboBox` instances explicitly declare `TData="string"` and `TValue="string"`.
- The three reported `PanelStudio` button classes are prepared as Razor variables and passed through simple `CssClass="@..."` expressions; the mixed literal/C# attribute forms are absent.
- `PageEffectStudio.CloseButtonAttributes` is a concrete `Dictionary<string, object>`, matching the DevExpress button attribute contract that produced the reported `CS1503`.
- The 4.0.6 Data Visual value-field formatting remains service-owned through `PanelStudioTextService.FormatList`; no direct `string.Join` was reintroduced into `PanelStudio.razor`.

## Source validation executed

Source-level checks were run without invoking the .NET SDK/MSBuild. They confirm the reported failure forms are absent, the affected controls retain their existing bindings and behavior, the exact text-service ownership regex reports no new direct string/regex operation, version identities are aligned, and the repository `build/` tree is byte-for-byte unchanged relative to the supplied 4.0.6 package.

## Environment limitation

The .NET SDK/MSBuild, PowerShell and GitHub were not used in this preparation environment. No restore/build/publish or runtime UI execution is claimed. The maintainer's .NET 10 + DevExpress build remains authoritative for compiler/runtime verification.
