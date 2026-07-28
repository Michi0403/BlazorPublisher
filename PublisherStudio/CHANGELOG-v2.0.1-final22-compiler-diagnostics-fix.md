# BlazorPublisher 2.0.1 final22 — compiler/diagnostics fix

## Fixed

- Converted `RequirePattern`, `ReadStore`, `Compile`, and `ValidateOptions` into instance methods so they use the injected structured logger without static state.
- Added explicit try/catch diagnostic boundaries to all four methods and rethrow every failure, preserving fail-closed object-store behavior.
- Kept pattern content and full paths out of diagnostics.
- Refreshed only the reviewed protected-architecture hash for `PanelStudioTextPatternDataService`.

## Preserved

- No PowerShell safeguard, diagnostic baseline, logging baseline, security rule, 1-Wire rule, service lifetime, runtime-value ownership rule, or serializable object-store boundary was changed or weakened.
- Pattern text, options, and timeouts still come exclusively from the seed/override object stores through `IPanelStudioTextPatternDataService`.
- No hidden fallback or component/controller-owned runtime value was introduced.

## Validation

- The full PublisherStudio Node source-contract suite passes, including the final22 compiler/diagnostics regression contract.
- The source-level reproduction of `Assert-MethodDiagnostics.ps1` reports zero new violations.
- The protected-architecture and final19 security-rule SHA-256 manifests validate.
- Native `dotnet` compilation and Windows PowerShell execution were unavailable in the packaging environment.
