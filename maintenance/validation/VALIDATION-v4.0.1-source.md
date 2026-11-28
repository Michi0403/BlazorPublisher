# PublisherStudio 4.0.1 source validation

## Scope

Source-only audit and repair. No `dotnet`, MSBuild, NuGet restore, build, publish, GitHub access, or online repository access was used.

The scan generalized the confirmed 4.0.0 Story/Text Studio fix across PublisherStudio: high-frequency browser/vendor interaction state must not be continuously round-tripped through InteractiveServer renders.

## Repository-wide finding and repair

Story/Text Studio remains on the confirmed 4.0.0 architecture: RichEdit document content is initialized one-way, selection is observed rather than rebound, selection notifications suppress the automatic component render, and editor generations reset stale transient state deliberately.

One additional same-shape risk was found in Media Studio. Its audio `DxRangeSelector` supplied selected range values from component state while `RangeSelectorValueChangeMode.OnHandleMove` sent every drag position back to the server. 4.0.1 changes that control to `OnHandleRelease`, leaving the live handle drag DevExpress-owned and committing the durable trim range at the interaction boundary. `PublicationTimeline` already used `OnHandleRelease` and required no runtime change.

PublisherStudio also contains 47 native live-preview range inputs. Those are not routed through the DevExpress range-selector path: the existing reviewed `publisherInterop.js` native-range lifecycle coalesces browser input and explicitly excludes RichEdit/DevExpress/DevExtreme-owned controls. The new guard verifies that this exception remains intact; commit-on-change/handle-release remains the preferred architecture for new controls.

## Permanent maintenance rule

The following enforcement was added and wired into the normal build-maintenance chain:

- `build/audit_transient_ui_state_ownership.py`;
- `build/Assert-TransientUiStateOwnership.ps1`;
- `Directory.Build.targets` target `AssertPublisherTransientUiStateOwnership`;
- retention checks in `build/Assert-DevExpressComponentRetention.ps1`; and
- the architecture/remediation contract in `AGENTS.md`.

The audit rejects complex editor live document/selection two-way binding, known transient DevExpress selection/caret/scroll-style two-way bindings, and `DxRangeSelector` handle-move server feedback. RichEdit/HtmlEditor observation callbacks must prevent their automatic event render from feeding the same transient state back into the vendor control. Diagnostics include the approved solution: one-way initialization, vendor/browser-owned live state, generation reset, explicit commit boundaries, and browser-owned/coalesced preview when continuous visual feedback is genuinely required.

Synthetic negative fixtures confirmed rejection of `DxRichEdit @bind-Selection` and `DxRangeSelector` `OnHandleMove`; the corresponding one-way/non-rendering observer pattern passed.

## Maintained source audits executed

The following source-only checks passed after the repair:

- `build/audit_release_4_0_1.py`;
- `build/audit_transient_ui_state_ownership.py --root . --product publisherstudio` — 49 Razor components passed; 47 reviewed native live-range inputs remain behind the required browser coalescer;
- `build/audit_application_architecture.py --root . --product publisherstudio --mode all`;
- `build/audit_async_only_architecture.py --source-root src/PublisherStudio.Web --product PublisherStudio` — 228 maintained source files passed;
- `build/audit_async_continuations.py --source-root src/PublisherStudio.Web` — 82 source files / 1,108 await tokens / 433 `ConfigureAwait(false)` / 621 renderer-affine `ConfigureAwait(true)` / 49 configured async disposals / 5 configured async streams;
- `build/audit_razor_maintenance_contract.py --root . --product publisherstudio` — 48 maintained Razor components passed;
- `build/audit_component_resilience.py --root .` — 3,041 component methods passed with zero legacy exemptions;
- `build/audit_service_resilience.py --root . --product publisherstudio` — 1,390 service methods plus 3 iterator/yield methods passed with zero exemptions/skips;
- `build/audit_prerender_interop_safety.py --root .` — 3,041 component methods checked, with 13 JavaScript-aware disposal methods attachment-gated;
- `build/audit_iterator_exception_policy.py --root .` — 3 iterator/yield methods passed;
- `build/audit_panelstudio_persistence.py` — passed, including its InteractiveServer boundary and JavaScript-diagnostics hash checks;
- `build/Assert-XmlDocumentationCoverage.py .` — 6,415 direct C# declarations across 259 maintained source files and 3,810 direct Razor `@code` members across 48 components passed; and
- Node `--check` for all 16 maintained top-level PublisherStudio browser JavaScript files.

Python syntax validation passed for the new generic and release audits. `Directory.Build.targets`, both active project files, `package.json`, and `package-lock.json` parse successfully. The 4.0.1 release audit also verifies the existing normalized JavaScript diagnostics manifest remains unchanged and valid.

PowerShell is unavailable in this preparation environment, so the new `.ps1` wrapper could not be executed through `pwsh`; its build wiring and retention tokens were source-checked, and the Python audit it invokes was executed directly.

## Release identity

Web, InstallerConsole, npm package/lock and browser cache identities are aligned at **4.0.1**, satisfying the repository's single-digit minor/patch rule. The source package excludes generated build/cache output.
