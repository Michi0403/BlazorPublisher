# PublisherStudio 2.5.4 — 1-Wire live capability sync and async-policy repair

## Version

- Rolled PublisherStudio Web and InstallerConsole from **2.5.3** to **2.5.4**.
- The shared `LocalGPT.WireProtocolVersion` dependency remains **2.1.1** because live directory refresh uses existing protocol messages.

## Panel Studio build-policy fix

- `SaveSelectedAsTemplate`, `SaveSelectedAsNewTemplate`, `FlushPanelStudioInteractionsAsync`, and `Save` now use `ConfigureAwait(false)` for the newly added non-lifecycle awaits.
- The Panel Studio interaction-lifecycle assertion and the source-only persistence audit were updated to require the same `ConfigureAwait(false)` policy while still requiring the browser queue flush to occur immediately before cloning module/panel state.
- The two renderer-lifecycle interop awaits that legitimately need renderer affinity remain `ConfigureAwait(true)`.

## Live 1-Wire organic capability synchronization

- PublisherStudio now answers `CapabilityRequest` and `SkillRequest` from a linked LocalGPT peer using the current catalog.
- The serializable deployed and user-local `publisher-dx-functions.json` files are watched at runtime. Catalog replacement/content changes raise a capability-directory notification.
- Organic permission save/delete operations raise the same directory-change notification so exposure/invocation policy changes are propagated too.
- `LocalGptConnectionService` coalesces those notifications with a bounded async signal and sends a refreshed `CapabilityResponse` over the existing linked 1-Wire connection only when the effective capability fingerprint changed. There is no reconnect and no one-second polling/log spam.
- `HelloAck` forces one fresh comparison so changes made while frontend link approval was pending are not lost.
- The advertised PublisherStudio application version now comes from the running assembly rather than stale hard-coded organic-wire version text.
- The static 1-Wire audit now checks the event-driven post-link synchronization wiring.

## Source-only validation

No `dotnet`, MSBuild, restore, build, publish, DocFX, or GitHub access was used. Architecture, service-resilience, documentation/1-Wire, Panel Studio persistence and XML-comment audits pass. The PublisherStudio async-continuation baseline was source-emulated and reports no failures; the Panel Studio lifecycle regex contract also matches the updated `ConfigureAwait(false)` requirement.
