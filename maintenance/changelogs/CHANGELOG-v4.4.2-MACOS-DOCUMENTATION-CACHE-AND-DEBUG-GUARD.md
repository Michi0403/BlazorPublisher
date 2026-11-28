# PublisherStudio 4.4.2 — macOS documentation-cache and Debug preflight repair

## Fixed

- `build/Build-Documentation.ps1` now puts Chromium's isolated `--user-data-dir`, disk cache, and crash-dump profile directories under `$documentationToolCacheRoot/browser-profiles`, instead of the old `bp` directory. This is the exact location already required by the existing `Assert-PowerShellCompatibility.ps1` source contract. It follows the configured repository-owned storage root on macOS, Windows and Linux; profiles remain unique and are cleaned using the existing process-shutdown and retry logic.
- `Directory.Build.targets` now invokes the full repository PowerShell compatibility preflight before the maintenance gate for **every ordinary, non-design-time PublisherStudio.Web build**, including Debug in Visual Studio and Release. Its existing checks still run in order; no skip flags or exemptions were added.
- `Build-LocalDevelopment.ps1` runs the same preflight before package preparation and restore, so a bad release-script dependency fails early instead of after a long documentation build.
- The existing `build/Assert-PowerShellCompatibility.ps1` was **strengthened**, not disabled: it now also rejects missing Debug/MSBuild and local-development preflight wiring.
- Both PublisherStudio.Web and PublisherStudio.InstallerConsole versions are increased from 4.4.1 to 4.4.2.

## Release scope and preservation

- Based on the **last delivered 4.4.1 source**, not the separate unfinished Panel/Div Studio 4.4.2 prototype. No experimental module-level 1-Wire functionality was merged while that work is postponed.
- No application logic, 1-Wire security or approvals, Picture Studio, browser JavaScript, or DevExpress components were modified.
- The ordinary Debug build continues to run its extensive application architecture gates. This change adds the previously release-only PowerShell/parser/repository-storage preflight; packaging/publish-only operations are **not** run in ordinary Debug.

## Verification boundary

- The changed XML project/target files parse; the documented profile directory matches the unchanged original mandatory browser profile policy; the strengthened guard contains source-level checks for Debug parity.
- Existing browser-view, Picture Studio canvas and GIF Node regressions pass. ZIP integrity and original-file preservation are checked during packaging.
- The actual PowerShell guard, macOS `pwsh Build-Release.ps1`, and .NET/DevExpress compilation **cannot be executed in this environment**; macOS validation remains necessary.
