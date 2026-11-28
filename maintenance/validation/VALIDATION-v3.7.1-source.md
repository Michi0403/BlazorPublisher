# PublisherStudio 3.7.1 source validation

## Validation boundary

This validation was performed against the complete PublisherStudio 3.7.1 source tree without running `dotnet`. The environment was used only for source/static checks and archive verification. GitHub and the GitHub API were not accessed.

## Passed checks

- `build/audit_application_architecture.py --root . --product publisherstudio --mode all`
- `build/audit_cross_platform_boundaries.py`
- `build/audit_async_continuations.py --source-root src/PublisherStudio.Web`
- `build/audit_service_resilience.py --root . --product publisherstudio`
- `build/audit_powershell_variable_interpolation.py`
- `build/audit_prerender_interop_safety.py --root .`
- `build/audit_component_resilience.py --root .`
- `node --check docs/templates/publisherstudio/public/main.js`
- `build/audit_release_3_7_1.py`
- XML parsing for maintained project/target files and JSON parsing for DocFX/package metadata through the release audit.
- Authored documentation theme bytes match both in-app help-doc copies and the tracked Pages archive through the release audit.
- InteractiveServer render-mode ownership remains on the established owner-component set; the Error page remains static.

## Release-specific source assertions

The 3.7.1 audit verifies one-digit version rollover, required documentation PDF configuration, viewport-contained dynamic sky, guaranteed satellites, fallback-sky handoff, disabled large blur-filter nebula objects, attached-node Mermaid rendering with SVG labels and bounded retries, Mermaid HTML source blocks, glass/gutter preservation, Pages/help-doc theme parity, and established render-mode boundaries.

## Not claimed

No compiler, runtime-browser, installer, publish, PDF-render, signing, notarization, or platform-package execution is claimed by this source-only validation.
