# PublisherStudio 2.5.0 — Localization duplicate-key integrity repair

## Version rollover

- PublisherStudio advances from `2.4.9` to `2.5.0` in accordance with the project version rule that the second and third version slots never reach two digits.

## Build/runtime failure repair

- Repaired all case-insensitive localization collisions introduced by the 2.4.9 coverage expansion: `Text.Date` / `Text.DATE`, `Text.Page` / `Text.PAGE`, and `Text.Time` / `Text.TIME`.
- The normal-title and uppercase source texts are both retained through distinct semantic source-text keys, so browser runtime translation still distinguishes `Date` from `DATE` without violating the case-insensitive catalog contract.
- English and German remain aligned at 3,035 maintained entries.
- `Assert-LocalizationIntegrity.ps1` remains enabled and unchanged; source-controlled case-insensitive duplicate keys still fail the build.

## Defensive localization loading

- `FileLocalizationService.LoadFile` no longer uses LINQ `ToDictionary(..., StringComparer.OrdinalIgnoreCase)` on a dictionary that may contain case-only duplicate JSON keys.
- Runtime/override catalogs are normalized entry-by-entry. If an external or legacy catalog bypasses the build guard, PublisherStudio logs the duplicate and uses the later value defensively instead of crashing during `GetAvailableCultures` / startup.
- The source guard remains the authoritative prevention layer; the loader change is only a runtime safety net.

## Versioned browser assets/documentation

- PublisherStudio.Web and PublisherStudio.InstallerConsole are `2.5.0`.
- Browser module cache keys and maintained documentation version labels were advanced to `2.5.0`.
