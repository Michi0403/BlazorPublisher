# PublisherStudio 4.0.5 source validation

## Scope

Focused code-side maintenance repair for the reported 4.0.4 Razor compiler, component diagnostics and application-static policy failures. No maintenance guard was weakened or bypassed.

## Confirmed source checks

- PublisherStudio Web, InstallerConsole, npm package/lock and browser cache-buster identities are aligned at **4.0.5**.
- `SystemFontPicker` declares `DxComboBox` as `TData="string"` / `TValue="string"`, retains custom user input and virtualized system-font data, and supplies `CssClass` through one parser-safe expression.
- `DevExpressColorPicker` supplies its optional class through one parser-safe expression and owns catch/log/user-notification handling for operational failures.
- `PageEffectStudio` and `PanelStudio` no longer declare application-level static enum option arrays; their option data is component-owned instance state.
- The publication model, saved document format, Panel Studio structural-renderer roots and existing DevExpress-first UI ownership remain unchanged.

## Source validation executed

The maintained Python architecture audit passes in static mode on this source tree. Additional source-level checks confirm that the reported mixed `CssClass` attribute forms are absent, the new color picker satisfies the component diagnostics catch/log/notification threshold, and version identities are consistent at 4.0.5.

## Environment limitation

The .NET SDK/MSBuild and GitHub are not used in this preparation environment. No restore/build/publish or runtime UI execution is claimed. The maintainer's .NET 10 + DevExpress build remains authoritative for compiler/runtime verification.
