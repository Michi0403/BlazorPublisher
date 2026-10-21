# PublisherStudio 4.3.3 — goal execution request handoff, roadmap step 1

## Scope and base

Based on the previously supplied PublisherStudio 4.3.2 source archive. Changes are restricted to the paired LocalGPT 6.1.5 goal-execution entry point and the source-only regression tests; this release does **not** implement roadmap steps 2–6.

## Implemented

- The existing `PublisherAiChatRequest` gains optional `RequiredCapabilities` for callers that already know the organic capabilities relevant to the user goal.
- `PublisherAiBridgeService.ChatAsync` forwards these capability names through the existing typed 1-Wire `RequestedOrganicCapabilities` field. Empty inputs preserve previous behavior; duplicate entries are removed. It does not introduce a second transport or unapproved executable command channel.
- LocalGPT 6.1.5 routes the received standard Council request through its new `IGoalExecutionService`. The bridge continues consuming the established Council result JSON, which now optionally contains the goal-execution summary, and continues to disallow browser self-approval of consequential operations.
- Added one source-level handoff test; updated existing version tests.
- Incremented Web and InstallerConsole versions **4.3.2 → 4.3.3**.

## Limitations and validation

- Goal resolution, provider selection and execution evidence are owned by LocalGPT, not duplicated inside PublisherStudio. No full media execution/serialization bus or crash-recovery behavior is claimed.
- Source-only Python tests: 5 passed. Existing Node GIF test: 1 passed.
- No PowerShell architecture script was altered or bypassed. No .NET/DevExpress build or runtime 1-Wire execution occurred in this environment. Validate the paired two-application deployment on a supported host.
