# PublisherStudio 3.0.0 source validation

This handoff was intentionally validated without invoking `dotnet` and without GitHub/network repository access.

## Static checks performed

- Parsed `appsettings.json`, `package.json`, `package-lock.json` and all six localization catalogs as JSON.
- Checked `mediaStudioInterop.js` with `node --check` when Node was available.
- Ran `build/audit_release_3_0_0.py` to verify version rollover, cache identities, recording insertion semantics, preview asset lifetime, DevExpress overview tick sizing, Ctrl/Command + Shift multi-selection, selected-range exports, logging configuration, render-mode ownership, localization parity and JavaScript diagnostics hashes.
- Refreshed `build/javascript-diagnostics-files.sha256` from normalized LF content.
- Confirmed PublisherStudio current-version references are 3.0.0 and the single-digit minor/patch rule is satisfied.

## Not performed

- No `dotnet build`, `dotnet test`, restore, publish, installer build or runtime execution was performed.
- No GitHub online access was used.

The source therefore requires the normal downstream .NET/runtime verification in the target environment before release deployment.
