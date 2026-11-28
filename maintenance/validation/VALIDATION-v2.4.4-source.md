# PublisherStudio 2.4.4 source validation

Scope: source-only validation. The requested workflow explicitly excludes .NET compilation, restore/publish, GitHub access, and online repository access.

## Evidence reviewed

- The supplied Panel Studio DOM shows the real `publication-panel-element` and its `panel-studio-hitbox` carrying the same normalized geometry while the DevExtreme chart inside the live element had initialized at a pathological `1439 x 1` SVG size. This isolates the observed selection mismatch to authoring/layout ownership and child-runtime sizing rather than corrupt persisted element coordinates.
- The supplied publication JSON keeps valid panel/page geometry, including fixed and responsive panel models and reusable Panel Library content. Existing website/image output demonstrates that the publication model and export renderers are substantially healthier than the Panel Studio authoring surface.

## Version checks

- PASS — `PublisherStudio.Web` version is 2.4.4.
- PASS — `PublisherStudio.InstallerConsole` version is 2.4.4.
- PASS — minor and patch slots remain single-digit.
- PASS — publication format version is unchanged because this release does not add/remove/rename persisted JSON fields.

## Panel Studio authoring geometry

- PASS — PanelView exposes `panel-force-canvas` when Arrange mode requests authored canvas coordinates.
- PASS — all responsive element/layout rules explicitly exclude `panel-force-canvas`; responsive CSS can no longer override authored X/Y/width/height during arrangement.
- PASS — PanelView marks the real `publication-panel-viewport` as the authoring coordinate owner and renders the optional `AuthoringOverlay` inside that viewport.
- PASS — Panel Studio selection hitboxes and drag/drop overlay are supplied through `AuthoringOverlay`, not as a sibling of the rendered panel.
- PASS — browser pointer/drop conversion resolves the marked authoring viewport before normalizing coordinates.
- PASS — the editor exposes the panel object's real 96-DPI design width/height and browser interop uniformly fits that design surface into the available workspace. The panel is no longer stretched independently in X/Y.
- PASS — the existing stable Panel Studio browser binding is retained across Arrange/Interact changes and now owns/disposes its layout ResizeObserver.

## DataVisual sizing

- PASS — `liveDataInterop.js` rejects degenerate render sizes below 4 x 4 pixels instead of constructing DevExtreme visuals from one-pixel layout measurements.
- PASS — every live DataVisual owns a ResizeObserver and repaints/resizes from the actual host size.
- PASS — DataVisual disposal disconnects ResizeObserver, cancels pending animation frames, clears polling timers and disposes the DevExtreme instance.
- PASS — `DataVisualClientHost` memoizes the serialized client configuration so unrelated Blazor parent renders do not recreate unchanged charts/grids while geometry is changing.
- PASS — maintained JavaScript diagnostics SHA-256 manifest was refreshed for the two changed browser runtimes.
- PASS — `node --check` succeeds for `liveDataInterop.js` and `publisherInterop.js`.

## Regression guard

- PASS — added `build/Assert-PanelStudioAuthoringGeometry.ps1`.
- PASS — `Directory.Build.targets` wires the new guard after the existing Panel Studio interaction-lifecycle guard and before JavaScript diagnostics.
- PASS — the guard rejects a future hit-layer split, raw responsive element rules that ignore force-canvas mode, missing DataVisual resize ownership, and removal of configuration memoization.

## Repository static audits executed

- PASS — `build/audit_application_architecture.py --root <repo> --product publisherstudio --mode all`.
- PASS — `build/audit_documentation_onewire_contracts.py`.
- PASS — `build/audit_service_resilience.py --root <repo> --product publisherstudio` (1243 service methods covered; expected yield/direct Program/Startup exclusions reported by the maintained audit).
- PASS — PublisherStudio project files and `Directory.Build.targets` parse as XML.
- PASS — all maintained JavaScript files match the diagnostics manifest and retain guarded diagnostics markers.
- PASS — localization JSON files parse successfully.
- PASS — edited Razor files pass source-level brace/structure sanity checks.

## Validation boundary

No `dotnet`, MSBuild, restore, publish, runtime Blazor compilation, GitHub call, or online repository access was performed. Therefore this package deliberately makes no compiler-clean claim. The changed JavaScript was syntax-checked directly with Node, and the maintained source audits above passed.
