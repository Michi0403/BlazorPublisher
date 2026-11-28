# PublisherStudio 4.0.0 source validation

**SOURCE-NOT-COMPILED.** This repair was prepared without running `dotnet`, MSBuild, NuGet restore/publish, Visual Studio compilation or a licensed DevExpress build. The user's Windows .NET 10 + DevExpress build remains the authoritative Razor/C# compilation and runtime gate.

## Reproduction represented by the repair

The supplied follow-up establishes that the remaining cursor/toolbar jump is tied to a Mainframe text component's lifecycle rather than the already-repaired doubled-text localization path:

1. the untouched default text can be opened/closed/reopened without the race if it has not been applied back to the frame;
2. after applying Story Editor content to a Mainframe component, reopening that component reproduces the race;
3. a newly inserted text box can reproduce it immediately; and
4. the formatting toolbar can jump through arbitrary values together with the caret, so the defect is treated as selection-state replay rather than a specific font-size mismatch.

The corresponding source architecture previously two-way bound both live RichEdit document bytes and live RichEdit selection through Interactive Server state. In addition, every selection callback caused ComponentBase to schedule another StoryEditor render.

## Repair verification

Static checks confirm:

- Story RichEdit uses one-way `DocumentContent="@_content"` initialization and no longer uses `@bind-DocumentContent`;
- Story RichEdit observes `SelectionChanged` but no longer passes `_selection` back through `@bind-Selection`;
- the selection observer marks its automatic ComponentBase render for suppression, and `ShouldRender()` consumes that suppression by returning `false`;
- selection state is reset when opening a different keyed story generation, closing Story Editor, and replacing a legacy document generation;
- Apply still exports the live RichEdit OpenXML/HTML directly before updating the Mainframe text component;
- the previous RichEdit DOM-localization exclusion and shared input/layout ownership protections remain present;
- no new-text font/default-format behavior was changed as part of this race repair;
- the build-wired interaction lifecycle guard contains regression checks for the live-state ownership and non-rendering selection-observer contract; and
- Web, InstallerConsole, npm package/lock and browser module/cache identities are aligned at 4.0.0, satisfying the single-digit minor/patch version rule.

## Maintained source audits executed

The following source-only checks passed after the repair:

- `build/audit_release_4_0_0.py`;
- `build/audit_application_architecture.py --root . --product publisherstudio --mode all`;
- `build/audit_async_only_architecture.py --source-root src/PublisherStudio.Web --product PublisherStudio`;
- `build/audit_async_continuations.py --source-root src/PublisherStudio.Web`;
- `build/audit_component_resilience.py --root .`;
- `build/audit_prerender_interop_safety.py --root .`;
- `build/audit_service_resilience.py --root . --product publisherstudio`;
- `build/audit_iterator_exception_policy.py --root .`;
- `build/audit_panelstudio_persistence.py`;
- `build/audit_razor_maintenance_contract.py --root . --product publisherstudio`;
- `build/Assert-XmlDocumentationCoverage.py .`; and
- Node `--check` for all maintained top-level PublisherStudio browser JavaScript files.

PowerShell is unavailable in this preparation environment, so the changed build-wired PowerShell guard could not be executed through `pwsh`; its newly added regular-expression requirements were source-checked against the final files, and the corresponding Python 4.0.0 release audit passed.

## Recorded static results

- application architecture policy: passed;
- async-only architecture: 228 PublisherStudio source files passed;
- async continuation policy: 82 source files / 1,108 await tokens, with 433 `ConfigureAwait(false)`, 621 renderer-affine `ConfigureAwait(true)`, 49 configured async disposals and 5 configured async streams;
- component resilience: 3,041 component methods, zero legacy exemptions;
- prerender interop safety: 3,041 component methods, 13 JavaScript-aware disposal methods attachment-gated;
- service resilience: 1,390 service methods plus 3 maintained iterator/yield methods, zero exemptions/skips;
- iterator exception policy: 3 iterator/yield methods passed;
- Panel Studio persistence architecture: passed;
- Razor maintenance architecture: 48 Razor components passed;
- C# XML documentation: 6,415 direct declarations across 259 maintained source files;
- Razor XML documentation: 48 component types / 3,810 direct `@code` members;
- JavaScript syntax: 16 maintained top-level browser files passed; and
- 4.0.0 live-state ownership/non-rendering-selection release contract: passed.

No generated binary output belongs in the source archive; `bin`, `obj`, `.git`, `.vs`, `node_modules`, Python cache files and test-result/build-output directories are excluded from packaging.
