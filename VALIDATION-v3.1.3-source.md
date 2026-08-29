# PublisherStudio 3.1.3 source validation

PublisherStudio 3.1.3 is a source-maintenance handoff. This environment intentionally does not use GitHub and has no .NET SDK or PowerShell runtime, so it does not claim a compiler or native-package build result.

The exact delivery ZIP is validated after fresh extraction. The maintained static gate requires these checks to pass on that extracted copy:

- `python3 build/audit_release_3_1_3.py`
- `python3 build/audit_cross_platform_boundaries.py`
- `python3 build/audit_application_architecture.py --root <root> --product publisherstudio --mode all`
- `python3 build/audit_async_continuations.py --source-root <root>/src/PublisherStudio.Web`
- `python3 build/audit_service_resilience.py --root <root> --product publisherstudio`
- `python3 build/audit_component_resilience.py --root <root>`
- `python3 build/audit_prerender_interop_safety.py --root <root>`
- `python3 build/audit_panelstudio_persistence.py`
- `python3 build/audit_iterator_exception_policy.py --root <root>`
- `python3 build/Assert-XmlDocumentationCoverage.py <root>`
- the repository system-variable-initialization rule mirrored byte-for-byte from `build/Assert-SystemVariableInitialization.ps1`
- XML/JSON source metadata parsing and Python build-script syntax compilation
- ZIP CRC, duplicate-entry, traversal-entry, and version-identity checks

The release audit specifically covers the two reported direct system-variable-name findings, the missing XML documentation in `LocalGPT.ReleasePackaging/Program.cs` and `OrganicReplayPolicyDataService.Snapshot`, the `Set-StrictMode` zero-PDF `.Name` failure in the Pages snapshot path, Debug HTML-only documentation behavior, the seven-runtime cross-platform release matrix, and the Unix/macOS package-format paths.

The supplied PublisherStudio 3.0.0 baseline is also compared for explicit Blazor render-mode boundaries. All five explicit `@rendermode` declarations present in that baseline remain present in 3.1.3, including all four maintained `InteractiveServer` pages and the browser-only diagnostics island.

A real Windows/macOS/Linux build remains the authoritative compiler and native-package test. Any compiler finding from that run should be treated as a release blocker and repaired in the next version.
