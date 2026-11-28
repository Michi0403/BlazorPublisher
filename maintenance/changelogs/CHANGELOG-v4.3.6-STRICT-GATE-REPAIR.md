# PublisherStudio 4.3.6 — strict architecture gate and Picture Studio release repair

## Source baseline

Based on PublisherStudio 4.3.5, retaining the repaired Picture Studio canvas render scheduler from 4.2.2 and subsequent editable GIF features. No behavior was reverted. No GitHub or .NET build was used. The architecture/assertion scripts (`Assert-*.ps1`), gate wiring, DevExpress retention contract, and strict logging/localization rules remain enabled and unchanged.

## Repairs for the supplied Visual Studio failures

- **PowerShell-only maintenance:** The previous 4.3.4 release contained three `tests/static/test_*.py` files. Overlay extraction kept these files even though 4.3.5 removed them from the archive. The existing `Migrate-ObsoletePythonMaintenance.ps1` now recognizes only those three historical test paths, preserving existing contents in `artifacts/.build-storage/retired-python-maintenance` before the strict no-Python guard examines the checkout. All three *original* SHA-256 fingerprints are in the migration inventory. Other Python test/scripts remain subject to the normal hard failure; no broad ignore/baseline was added.
- **Localization integrity:** Both the GIF operation label (`Convert visible layers to GIF frames`) **and** its success notice (`Converted visible layers to GIF frames`) have canonical `Phrase.*` keys in all six maintained locale catalogs (English, German, French, Spanish, Japanese and Ukrainian). German strings are genuinely translated. Existing `LT` calls are retained.
- **Typed Razor component diagnostics:** Restore exactly one `@inject ILogger<MediaStudioTitlebar> Logger` directive without changing its header or close callback. All component `.razor` files are checked for exactly one typed logger.
- **Service method resilience:** `OrganicWorkExecutor.SupportsCapability` now has its own `try/catch`, structured diagnostic logging and rethrow for unexpected errors. It still uses its original native/DI extension capability logic.
- **JavaScript diagnostics SHA-256:** Update `build/javascript-diagnostics-files.sha256` to match the actual 4.3.5 `pictureStudioInterop.js` content. No rendering, gesture, GIF or upload code was reverted to fix the hash check.
- **Release identity:** Bump Web **and** InstallerConsole to 4.3.6; update Picture Studio JavaScript module query versions to 4.3.6 to avoid loading cached stale browser code.

## Verification

- 17/17 maintained JS hashes/markers/error-reporting paths matched the existing diagnostics contract.
- All six localization catalogs have the same 3,439 keys, no case-insensitive key duplicates and complete literal `LT`/`GetText` coverage; German GIF translations are distinct from English.
- All current Razor components have exactly one typed logger directive.
- No Python maintenance files in the source ZIP. The three known historical Python tests match the migration inventory by exact SHA-256 and filename.
- Both `node tests/js/picture-studio-gif-frames.test.mjs` and `node tests/js/picture-studio-canvas-regression.test.mjs` passed; Picture Studio/animated GIF JavaScript parse checks passed.
- `OrganicWorkExecutor.SupportsCapability` contains a method-local logged catch boundary.
- **Not performed:** Windows PowerShell architecture gates, .NET compilation/DevExpress build or live Blazor interaction. Validation is source-only plus the actual Node regressions, not a claim that the Windows build passed.
