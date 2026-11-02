# PublisherStudio 4.7.2 — LocalGPT organic connection build repair

## Fixed

- `LocalGptConnectionService.ConnectByWebUrlAsync` now has method-local exception handling and structured ILogger diagnostics. It rethrows the original exception so the manual connection page can continue trying other entered origins. HTTP/HTTPS origin validation, certificate verification, remote identity/origin checks, MFA approval and the existing 1-Wire transport remain unchanged.
- `OrganicPlugins.razor` no longer directly splits manual LocalGPT origin text or joins connection error strings. It uses the existing injected `PublicationEditorTextService`, preserving ordered case-insensitive deduplication and the previous per-origin error notifications. The text service logs its own failures.
- The `DxCheckBox` opt-in auto-reconnect callback now uses an explicitly typed `bool` lambda, consistent with the other maintained DevExpress checkbox bindings; this addresses the reported `CS1503` and cascading Razor-generated `CS1662` errors at `OrganicPlugins.razor:146`.
- The browser-local reconnect preference handler now reports persistence failures through ILogger and IUserNotificationService without incorrectly confirming a new setting that could not be saved.
- The early mandatory `ONWIRE-READY` source guard now checks these specific connection, callback and service-ownership contracts, so their removal fails before Razor compilation.
- Synchronized the Web and Setup project versions from 4.7.1 to 4.7.2.

## Unchanged

- Manual LocalGPT HTTP/HTTPS discovery, UDP discovery, saved peer trust, 1-Wire transport authentication, human approval and capability negotiation.
- LocalGPT source is not modified. PublisherStudio remains compatible with the existing LocalGPT protocol package.

## Verification status

Offline source contracts, original-versus-modified diff scope, version XML, preflight checks and ZIP CRC can be checked without any .NET tools. There is **no claim of a successful PowerShell run, .NET compile, Visual Studio build or end-to-end 1-Wire session**: these require the operator's Windows/macOS environment.

## Interaction / frontend release checklist

- No new object, editor mode, toolbar, overlay, renderer, stacking context or input routing was added.
- Existing DevExpress checkbox and page layout are retained; only the callback expression changes.
- Browser localStorage failures now produce a user notification and structured log; reconnect is still explicit opt-in.
- Existing security, cancellation, preview and publication rendering paths are unchanged.
- New source regression checks cover the reported C# callback, manual origin parser ownership and service resilience defects.
