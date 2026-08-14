# PublisherStudio 2.6.6 — Strict async policy and build repair

## Zero-tolerance async continuation policy

- Replaces the historical reviewed-count/baseline model with a syntax-aware invariant: every maintained `await` must explicitly choose `ConfigureAwait(false)` or the renderer-affine `ConfigureAwait(true)` path.
- `await foreach` must explicitly configure the async enumerable.
- `await using` must use an explicitly configured async disposal; language-level implicit async disposal is no longer grandfathered.
- The active `async-continuation-baseline.json` has been removed. New raw awaits cannot be accepted by increasing a numeric baseline.
- The build guard requires the repository's Python 3 syntax-aware audit instead of silently falling back to a weaker regex/count check.
- `ConfigureAwait(true)` is accepted only in PublisherStudio component/renderer source. Service/controller/background source must use `ConfigureAwait(false)`.
- 834 previously noncompliant async constructs were migrated while preserving their intended UI/background continuation ownership.

## Build-policy repair

- Fixes the 2.6.5 build failure where `MediaStudio.razor` and `Editor.razor` exceeded the old unconfigured-await count baseline. The source itself now satisfies the strict policy instead of enlarging that baseline.
- Fixes the Inspector text-service ownership failure by moving the `ValueFields` display join into `PublicationComponentService.JoinDisplayValues`. The component no longer performs direct string joining.
- Adds a 2.6.6 regression audit that rejects restoration of the legacy await baseline and verifies the strict continuation policy and ownership repair.

## Compatibility

- PublisherStudio Web and InstallerConsole are 2.6.6.
- Publication format remains 1.58.
- Picture Studio format remains 1.5.
- No database migration is required.
- Media Studio/Picture Studio behavior from 2.6.5 is retained.
- RichEdit/word-processing was not changed.
