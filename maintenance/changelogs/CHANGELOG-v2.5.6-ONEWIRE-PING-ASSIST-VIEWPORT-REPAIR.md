# PublisherStudio 2.5.6 changelog

## Reliable 1-Wire connection test

- Replaced the heavy capability-directory round-trip test with the protocol's lightweight `Ping` → `Pong` exchange on the already linked connection.
- `Pong` now satisfies the same correlation waiter used by other 1-Wire replies.
- The test reports the negotiated protocol version and current capability count after a successful transport round trip; capability synchronization continues independently through the existing live post-link directory synchronization.
- `WaitForResultAsync` now distinguishes an internal response timeout from caller cancellation. A genuine response timeout becomes a descriptive `TimeoutException`/warning rather than the misleading `TaskCanceledException` service failure previously shown to the user.

## LocalGPT AI Assist viewport and workflow

- Retained the simplified **LocalGPT AI Assist** primary workflow from 2.5.5: selected object/page context, target, profile, prompt and optional LocalGPT team/preset override.
- Advanced custom Council execution, security/trust, negotiated capability/route details and diagnostics remain available behind expandable advanced sections rather than dominating the creative workflow.
- Hardened the `/organic-plugins` scroll container for PublisherStudio's fixed-height application shell with `min-height:0`, stable vertical scrolling, overscroll containment, safe-area bottom padding and a sticky connection/status strip.
- Removed the Publisher-side hard-coded maximum of eight from the advanced custom Council `Parallel models` input. LocalGPT's advertised/runtime policy remains authoritative.

## Existing behavior retained

- Automatic post-link capability synchronization and runtime capability/skill refresh remain intact.
- Panel Studio geometry, queued interaction flushing and reviewed async-continuation behavior remain intact.
- Generated/reviewed AI text continues through PublisherStudio's normal document/export pipeline; no unauthenticated LocalGPT loopback endpoint is exposed to arbitrary exported pages.

## Version

- PublisherStudio Web/installer: **2.5.6**.
- The consumed LocalGPT 1-Wire protocol package remains **2.1.1**; Ping/Pong already exists in that contract.
