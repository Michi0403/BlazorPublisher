# PublisherStudio 3.9.7 source validation

## Scope

Source-only validation of the Panel Studio geometry/inspector repair and shared editor-input ownership changes. No .NET compilation, MSBuild, restore, publish, GitHub access, or online repository access was performed.

## Evidence-driven repair

The supplied publication data showed manually inserted Panel Studio components occupying heavily overlapping center positions, while library/preset panels already contained distributed authored geometry. The supplied runtime DOM also showed publication panel elements wrapping their actual renderer inside Razor and DevExpress FormLayout maintenance shells, while the older panel sizing CSS depended on direct child render roots.

The repair therefore preserves the DevExpress maintenance architecture and fixes the two broken assumptions: wrapper-depth-safe child geometry and shared overlap-aware insertion placement.

The supplied cursor/input recurrence was treated as a shared ownership problem rather than a Text Studio special case. Slider drags, text/caret navigation, DevExpress/DevExtreme editors, RichEdit and scroll controls now suppress competing canvas/controller ownership for the duration of their focus or pointer lease.

## Static source audits

The following repository audits were executed directly and passed:

- application architecture (`audit_application_architecture.py --mode all`);
- async continuation policy: 82 source files, 1,108 await tokens, 433 `ConfigureAwait(false)`, 621 renderer-affine `ConfigureAwait(true)`, 49 configured await-using disposals and 5 configured async streams;
- async-boundary architecture: 228 maintained PublisherStudio component/service source files, 0 findings;
- component resilience: 3,039 component methods, 0 legacy exemptions;
- service resilience: 1,390 service methods plus 3 iterator/yield methods, 0 exemptions/skips;
- Razor maintenance architecture: 48 Razor components;
- Panel Studio persistence/interaction hash audit;
- data/panel/media source audit;
- cross-platform boundary audit: 60 checks, no platform leaks;
- iterator exception policy;
- prerender JavaScript interop safety;
- PowerShell variable-interpolation audit.

`publisherInterop.js` was syntax-checked with Node as an ES module. Its normalized SHA-256 entry in `build/javascript-diagnostics-files.sha256` was refreshed and the Panel Studio persistence audit accepted the manifest.

The augmented PowerShell Panel Studio guard expressions were also checked statically against the maintained sources because PowerShell itself is not available in this environment.

## Release integrity

PublisherStudio project/package/cache-busting references are versioned as 3.9.7. Historical 3.9.6 changelog/validation documents remain unchanged. The final source archive is checked separately for archive integrity and generated Python cache artifacts are excluded.
