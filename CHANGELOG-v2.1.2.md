# PublisherStudio 2.1.2

## Application language selection

- Added an always-visible application language selector to the shared PublisherStudio layout.
- Reused the flat JSON catalogs under `src/PublisherStudio.Web/Localization`.
- Aligned culture selection with LocalGPT: query-string culture values are handled first, the persisted request-culture cookie is handled second, and stale culture query values are removed before redirecting.
- Kept the existing per-publication language setting in Inspector; application language and publication metadata remain separate choices.
- The installer console remains a dependency-light executable and does not load the web localization system.

## Build-policy repair

- Restored the maintained logging-integrity policy source required by the enabled Windows build guard.
- The policy remains strict: structured diagnostics and their baselines may not be silently removed or weakened.

## Versions

- PublisherStudio application and setup: `2.1.2`.
- LocalGPT 1-Wire protocol package: `2.1.1`.
