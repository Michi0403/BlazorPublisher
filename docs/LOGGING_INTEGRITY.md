# Logging integrity

Logging removal is not cleanup; maintained diagnostics are monotonic.

PublisherStudio operational services, controllers, hosted services, and platform facades keep structured diagnostics at their failure boundaries. Refactoring an implementation behind a platform abstraction may move a boundary, but it must not silently remove the effective logging coverage. The committed `build/logging-baseline.json` records the reviewed minimum metrics for maintained source files, and `build/Assert-LoggingIntegrity.ps1` enforces that baseline during supported builds.

Baseline changes are maintenance changes: update only the entries whose implementation shape was deliberately reviewed, and keep runtime logging at or above the resulting boundary.
