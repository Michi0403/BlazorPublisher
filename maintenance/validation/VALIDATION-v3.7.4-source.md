# PublisherStudio 3.7.4 source validation

## Scope

Source-only validation of the API-reference left-navigation padding repair and synchronized 3.7.4 release metadata. No .NET build, restore, publish, signing/notarization, GitHub access, GitHub API operation, or other online repository access was performed.

## Checks performed

- `python build/audit_release_3_7_4.py` — passed.
- `python build/audit_component_resilience.py --root .` — passed; 2,687 component methods were checked with no legacy exemptions.
- `python build/audit_cross_platform_boundaries.py --root .` — passed; 60 maintained boundary checks reported no platform leaks.
- `python build/audit_prerender_interop_safety.py --root .` — passed.
- `python build/audit_service_resilience.py --root . --product publisherstudio` — passed; 1,382 service methods and the maintained iterator cases were checked.
- PublisherStudio Web and installer `.csproj` files parse as well-formed XML.
- Maintained documentation CSS is byte-identical to both shipped help-doc stylesheet copies.
- Generated help HTML references the current stylesheet hash, and the tracked Pages ZIP passes `ZipFile.testzip()` with synchronized CSS and the 3.7.4 handbook reference.

## Reported regression coverage

- API/ManagedReference navigation now receives the same left-navigation padding as conceptual Overview/Guide pages without broadening the layout selector to unrelated content.

## Limitation

This validation is intentionally not a compiler or runtime claim. The supplied instruction explicitly excluded invoking a .NET environment, so the change was validated through repository static audits and artifact consistency checks only.
