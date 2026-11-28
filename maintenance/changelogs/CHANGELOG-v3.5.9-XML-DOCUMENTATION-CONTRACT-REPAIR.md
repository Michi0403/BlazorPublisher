# PublisherStudio 3.5.9 — XML documentation contract repair

PublisherStudio 3.5.9 is a maintenance-only follow-up to 3.5.8. It addresses the compiler XML-documentation warnings and the strict documentation-quality audit reported by the 3.5.8 rebuild without changing runtime, installer, rendering, localization, or logging behavior.

- `StopInstalledPublisherStudioForUpdate` now documents its actual three-parameter contract: the canonical installation root, architecture-specific runtime folder, and diagnostic logger. Stale tags for the removed archive/target/runtime-identifier parameters were deleted, eliminating CS1572/CS1573 warnings from that method.
- `FileLoggerSharedSink.Dispose` now documents the real shutdown semantics: complete the bounded queue, allow the provider-owned writer to drain pending entries, then release queue resources.
- `FileLoggerProvider.Dispose` now documents provider ownership of the shared sink and the single-writer drain behavior instead of using a short generic `Stops ...` summary rejected by the maintained XML documentation quality policy.
- The documentation validator is not relaxed, bypassed, or rebased. The implementation documentation is corrected to satisfy the existing contract.
- The 3.5.8 installer compile fix, unchanged logging-integrity baseline, shared single-writer sink, renderer-affinity policy, Story Editor one-shot attachment guard, and LocalGPT-aligned overlay installer remain intact.

No production method body, public API signature, render mode, localization key, logging baseline, installer deployment contract, or architecture policy was intentionally changed in this release.
