# PublisherStudio 2.8.4 source changelog

## Compile repair

- Fixed the `PageSurface.razor` 2.8.3 regression where `selectedElementIds` was removed while the JavaScript initialization payload still referenced it.
- `PublicationEditorTextService` now owns both deterministic selected-element identifier formatting and the compact selection synchronization key. The Razor component consumes the normalized array and no longer performs the text manipulation that triggered the 2.8.2 ownership guard.
- Canvas initialization setup is now inside the existing operational exception boundary. Normal circuit cancellation/disposal is debug-logged; renderer readiness is warning-logged; JavaScript and unexpected failures are error-logged and surfaced through PublisherStudio notifications.

## Architecture maintenance aligned with LocalGPT

- Added `Assert-ComponentSafety.ps1`, wired into `Directory.Build.targets` before method diagnostics.
- Added a method-granular Razor resilience audit and explicit legacy baseline. Existing legacy component methods remain tracked rather than being mass-rewritten in a high-risk release; **new Razor methods cannot be added without their own try/catch plus structured logging boundary**. Python is mandatory for this guard so the build cannot silently weaken it.
- The existing global `OperationalErrorBoundary`, global logger factory and notification service are now asserted as maintained component-safety boundaries. `OperationalErrorBoundary.OnErrorAsync` itself now owns a defensive try/catch.
- Added `Assert-ServiceArchitecture.ps1`, mirroring LocalGPT's DI/static-state/asynchronous-ownership maintenance checks and asserting that PublisherStudio's existing all-service `audit_service_resilience.py` remains wired.
- PublisherStudio's broad service audit remains strict: service methods require try/catch plus diagnostics, and iterator/yield methods require try/finally plus diagnostics.

## Compatibility

- The 2.8.2 media, overlay interaction, slider, Converter Studio and rendered Video Studio changes remain intact.
- The reviewed five InteractiveServer render boundaries are unchanged.
- LocalGPT 1-Wire protocol package remains 2.1.1.
- Web and Installer Console versions are 2.8.4; active browser cache tokens were rolled to 2.8.4 / 20260818-284.
