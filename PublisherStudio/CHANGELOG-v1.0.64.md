# PublisherStudio 1.0.64

## Streaming contract cleanup

- Removed the leftover Service-local `PublisherStudio.Services.Streaming.MediaHost.MediaHostHotkeyEvent` declaration.
- Kept `PublisherStudio.Domain.Streaming.MediaHostHotkeyEvent` as the single authoritative hotkey event contract.
- Changed `StreamingMediaHostClient.ReadEventsAsync` to return the canonical Domain event list directly instead of creating an identical in-process mapping type.
- Resolved the reported CS0104 ambiguities in `GlobalHotkeyService`, `MediaSessionRegistry`, `StreamingSessionService` and `StreamingSessionUseCases`.
- Removed the source/assembly identity mismatch that produced the CS1503 conversion error after the v1.0.63 reorganization.

## Contract-ownership architecture rule

- Added an explicit one-contract/one-owner rule to `AGENTS.md` and the architecture documents.
- Shared requests, events, state and results crossing Components, Controllers, Hubs, Services or HostedServices must be declared once under the established `Domain` or `Models` owner.
- In-process facades may not introduce same-named Service copies of shared contracts.
- Provider-, protocol- or process-specific DTOs remain allowed only at real external boundaries, must have an explicit boundary name and must be mapped once.
- Added ADR-006 for canonical shared-contract ownership.

## Automated prevention

- Added `csharpContractOwnership.test.mjs`.
- The new suite rejects Services shadowing Domain/Models contracts.
- It treats namespaces imported by `GlobalUsings*.cs` as one shared symbol scope and rejects duplicate simple type names before they can produce CS0104.
- It verifies that `MediaHostHotkeyEvent` has exactly one declaration and that the in-process media-host facade reuses it directly.

## Compatibility

- Streaming routes, OAuth, provider output, recording, LAN delivery, hotkeys and editor behavior are unchanged.
- Publication format remains `1.47`; Picture Studio format remains `1.2`.
- Source archives contain no `bin` or `obj` output. Existing v1.0.63 working copies should clean those folders before the first rebuild so Visual Studio cannot retain the removed type in a design-time assembly.

Application and installer version: `1.0.64`.
