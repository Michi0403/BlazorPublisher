# PublisherStudio 2.2.6 — service resilience and DocFX shell repair

This source repair stays on the 2.2.5 application/API-documentation baseline and changes wiring and presentation rather than replacing working subsystems.

## Service diagnostics

- Every parsed method under `src/PublisherStudio.Web/Services` now owns a `try/catch` and a diagnostic write unless it contains `yield` or is directly used during `Program.cs` startup.
- Existing `ILogger` fields/parameters are reused. Service types without logger injection use `System.Diagnostics.Trace.TraceError`, avoiding a broad constructor/DI rewrite.
- Failures are logged and rethrown so higher operational boundaries can recover without silently committing partial state.
- `build/audit_service_resilience.py` verifies the rule and is invoked from `Assert-MethodDiagnostics.ps1` when Python is available.

## Documentation frontend

- `PublisherStudio.Web.styles.css` is now loaded by the Blazor shell. This activates existing scoped component CSS, including the already-built native `DocumentationViewerHost` dialog.
- The HTML guide therefore opens as the intended large focus-managed modal rather than an unstyled tiny browser `<dialog>`.
- PublisherStudio Kawaii CSS now uses the newer full-width shell already present in the source tree: visible left navigation, wide center article, independent right “In this article” rail, real spacing, and styled snapshot navbar links.
- The newer root-documentation-rail JavaScript is synchronized across authored template and shipped snapshot copies, matching the proven LocalGPT DocFX navigation behavior.
- API-reference generation and Pages publishing contracts are retained.

## JavaScript

- Maintained Kawaii documentation functions now have logged error boundaries. Existing PublisherStudio JavaScript diagnostics/callback guards remain the primary runtime protection for application JS.

## Versioning

The application/installer/package metadata is 2.2.6. The checked-in generated 2.2.5 documentation/PDF is intentionally not relabeled; the next owner-side .NET/DocFX build will regenerate 2.2.6 documentation from the authored sources.
