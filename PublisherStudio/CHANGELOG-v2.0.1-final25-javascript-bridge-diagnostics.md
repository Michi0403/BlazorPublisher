# BlazorPublisher 2.0.1 final25

## Fixed

- `JavaScriptDiagnosticsBridge.razor` now provides a user notification when the browser-to-.NET diagnostics bridge cannot attach.
- The component therefore satisfies the existing catch, structured logging, and notification architecture policy without exempting or weakening the policy.

## Preserved

- JavaScript errors remain mirrored to the Visual Studio application output through `ILogger`.
- Existing final19 security and 1-Wire preservation rules, runtime-value ownership, object-store pattern ownership, and JavaScript diagnostics requirements remain unchanged.
