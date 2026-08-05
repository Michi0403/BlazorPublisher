# Logging integrity policy

PublisherStudio keeps operational diagnostics as part of the product contract.

**Logging removal is not cleanup.** Maintained service, controller, hosted-service, and browser boundaries must keep their structured diagnostics unless a reviewed replacement provides equal or better evidence.

## Maintained rules

- Use injected `ILogger<T>` instances for operational services and controllers.
- Preserve exception objects when an error is logged.
- Keep request, startup, shutdown, browser bridge, installer, and optional 1-Wire boundaries observable.
- Deterministic instance helpers may use the reviewed `logging-policy: pure-helper` marker when they intentionally allow exceptions to reach the owning logged boundary.
- Refreshing the logging baseline requires an explicit maintainer action; ordinary builds may not rewrite it.

This file is an internal build-policy source and is intentionally not part of the public DocFX table of contents.
