# Async and component diagnostics policy

PublisherStudio keeps browser-renderer continuations and background/service continuations separate without rewriting working code mechanically.

- Razor/component code must never introduce `ConfigureAwait(false)`. Explicit component continuations use `ConfigureAwait(true)`.
- Service and controller `ConfigureAwait(false)` calls are monotonic and may not be removed as cleanup.
- Existing unconfigured awaits are a reviewed ceiling per file. New files must configure their awaits explicitly.
- Existing component catch, structured-log, and user-notification coverage is monotonic.
- New operational components require a catch/log/notification boundary. Normal `Dispose`/disconnect paths remain exempt.
- The reviewed page-level `InteractiveServer` boundaries are build protected and must not be replaced by an accidental global router boundary.
