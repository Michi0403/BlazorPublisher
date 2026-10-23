# PublisherStudio release task ledger

## 4.3.4 — Paired LocalGPT semantic-goal recovery

- **Closed (source implementation, runtime unverified):** preserve optional stable LocalGPT goal identity in the existing Council result bridge. Evidence: `PublisherAiBridgeService.cs`; `tests/static/test_goal_identity_release.py`.
- **Deferred:** full typed PublisherStudio media/render/publish execution handlers beyond existing 4.3.3 interfaces; requires service-by-service capability mapping and runtime verification.
- **Deferred:** direct PublisherStudio UI recovery control. No unapproved or browser-self-attested retry may be added; local operator approval must be designed before exposing such an action.
- **Deferred:** end-to-end multi-application runtime and DevExpress validation; unavailable in this source-only environment.

## 4.4.2 — macOS PowerShell release preflight repair

- **Closed (source change, macOS execution not verified):** make the Chromium documentation browser profile root `browser-profiles` beneath the configured repository-owned documentation cache, matching the existing release compatibility policy. Evidence: `build/Build-Documentation.ps1`; enforced by `build/Assert-PowerShellCompatibility.ps1`.
- **Closed (source change, Debug execution not verified):** wire the same non-optional compatibility preflight into non-design-time Visual Studio/MSBuild Web builds and the direct local-development entrypoint. Evidence: `Directory.Build.targets`, `Build-LocalDevelopment.ps1`, and the added parity assertion in `build/Assert-PowerShellCompatibility.ps1`.
- **Deferred:** Panel/Div Studio module-level organic functions, DevExpress AI routing, Report Designer and embedded offline AI in exported sites. The separate unfinished Panel/Div Studio 4.4.2 prototype is intentionally not part of this build-fix baseline.
- **Deferred:** reproduce the reported simultaneous LocalGPT/PublisherStudio debugger conflict with the actual Visual Studio launch configurations; the available logs prove only successful individual application startup/build.
