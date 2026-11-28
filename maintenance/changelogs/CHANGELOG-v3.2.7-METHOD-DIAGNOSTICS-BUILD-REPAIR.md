# PublisherStudio 3.2.7 — method diagnostics build repair

## Fixed

- Repaired the authoritative release build failure in `ApplicationPathService.KnownFolderOrFallback`.
- The helper is now an instance method so it can use the service logger and follows PublisherStudio's maintained service-resilience rule: each service method owns an explicit `try/catch` boundary and structured diagnostics.
- The path-selection behavior itself is unchanged: the operating-system known folder is still preferred and the existing per-user PublisherStudio fallback is still returned when the known folder is unavailable.

## Preserved from 3.2.6

- Future2/open-source positioning and DevExpress licensing clarification remain intact.
- macOS physical-architecture/Rosetta diagnostics, exact Mach-O architecture manifests, package validation, and the working installed-app launcher behavior remain unchanged.
- The reviewed `InteractiveServer` render-mode map remains unchanged.

## Version

- Version advanced from 3.2.6 to 3.2.7 because application source changed.
