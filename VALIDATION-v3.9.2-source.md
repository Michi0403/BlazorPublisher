# PublisherStudio 3.9.2 source validation

## Validation boundary

This handoff was prepared and reviewed without `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online repository access. Runtime/build confirmation remains for the licensed Windows development machine. PowerShell Core was not available in the source-review environment, so the maintained JavaScript manifest/forbidden-runtime checks were reproduced directly instead of executing `Assert-JavaScriptDiagnostics.ps1` itself.

## Passed source checks

- `python3 build/audit_razor_maintenance_contract.py --root . --product publisherstudio`
  - passed for 48 Razor components;
  - confirms the maintained component/layout ownership contract;
  - confirms popup bodies expose the real studio root directly;
  - confirms native/manual modal owners remain absent;
  - confirms the loaded `site.css` still carries the box-neutral wrapper and popup viewport contracts.
- `python3 build/audit_application_architecture.py --root . --product publisherstudio --mode all`
  - passed the maintained application/static/method/runtime/structure policy checks.
- Python syntax compilation passed for the changed maintenance audit scripts.
- Node syntax validation passed for all 16 maintained `wwwroot/js/*.js` files.
- The complete JavaScript diagnostics SHA-256 manifest matches all 16 maintained browser files after refreshing `javascript-diagnostics.js`.
- `javascript-diagnostics.js` contains none of the forbidden global replacements for `EventTarget.addEventListener/removeEventListener`, timers, requestAnimationFrame, queueMicrotask, or observer constructors.
- JSON parsing and current version checks passed for `package.json` and `package-lock.json`.
- XML parsing and current version checks passed for PublisherStudio Web and InstallerConsole project files.
- All 17 maintained `DxPopup` `BodyContentTemplate` roots were source-checked: none has a maintenance `DxFormLayout` between the DevExpress modal body and its real dialog/studio surface.
- The universal 1800×1180 popup shell no longer exists in the maintained Razor components; workbench popups now use their established responsive dimensions.
- 14 standalone popup/studio components are visibility/readiness-gated before their maintenance layout boundary, so closed studios do not render empty DevExpress FormLayout item trees.
- InteractiveServer directive comparison is equivalent in placement/value between 2.9.8, 3.7.6, 3.9.1, and 3.9.2: the same five PublisherStudio Razor files own explicit render-mode directives.
- `src/PublisherStudio.Web/Services` is byte-identical to 3.9.1.
- Regression source tokens remain present for:
  - recovered Mainframe/page rulers;
  - Data Visual readable chooser captions and horizontal checkbox rows;
  - rectangular-shape `CornerRadiusMm` in Mainframe and print/export rendering;
  - exported presentation `.ps-controls` and document `data-playback-controls` behavior;
  - Page Effects internal-scroll surface.
- Version `3.9.2` satisfies the no-two-digit minor/patch-slot rule.

## Evidence-directed repair checks

The supplied 3.9.1 runtime evidence showed normal host startup and successful Spreadsheet HTTP requests before repeated DevExpress browser `slotchange` failures. 3.9.2 therefore changes only the frontend lifecycle/geometry layer around those surfaces: it removes the extra popup-body FormLayout hop, reduces hidden studio DOM, restores per-studio popup dimensions, makes browser diagnostics observational, and gives Panel Studio a bounded portal-materialization retry. No service implementation was changed to mask a frontend failure.

## Not claimed

No .NET compilation, Razor compilation, browser launch, DevExpress runtime execution, installer build, publish, or deployment was performed in this environment. Those must be confirmed by the recipient build/test run.
