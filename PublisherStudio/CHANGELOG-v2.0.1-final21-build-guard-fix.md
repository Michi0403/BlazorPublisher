# BlazorPublisher 2.0.1 final21 — build-guard fix

## Fixed

- Marked `PanelTextPatternStoreOptions` with the existing `logging-policy: pure-helper` classification. The class is a passive options DTO and contains no operational behavior that should introduce logging or exception handling.
- Updated the reviewed protected-architecture hash for that source file.

## Preserved

- The logging integrity guard and its monotonic baseline are unchanged.
- No security rule, 1-Wire rule, runtime-value ownership rule, object-store boundary, or service registration was weakened.
- Panel regex text, options, and timeouts remain supplied by serializable object storage through `IPanelStudioTextPatternDataService`.

## Validation

- The final20 logging-integrity and security-policy files are byte-for-byte equivalent after normalized line endings.
- The final19 security hash manifest and final21 protected-architecture manifest validate.
- The logging-integrity source scan reports zero new failures.
- Native `dotnet` compilation was not available in the packaging environment.
