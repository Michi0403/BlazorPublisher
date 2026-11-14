# PublisherStudio 4.7.8 — enum-aware 1-Wire capability refresh

## Observed failure

Windows PublisherStudio 4.7.7 received a 1-Wire `CapabilityResponse` (651,412 bytes), then failed in `PublisherAiBridgeService.ChatAsync` while parsing `Capabilities`: `System.Text.Json.JsonException` at `$[0].InteractionEditor` (line 143). The error occurred before `SendCouncilRequestAsync` could run, so the embedded PublisherStudio AI Chat component could not produce a response.

## Cause

`PublisherAiBridgeService` introduced a separate `JsonSerializerOptions(JsonSerializerDefaults.Web)` instance for capability-response parsing. That serializer does not include the `JsonStringEnumConverter` used by PublisherStudio's authoritative `IOrganicPluginProtocolCodec`. LocalGPT sends named values of `OneWireInteractionEditor` through the established 1-Wire contract, so the ad-hoc parser rejected valid response data.

## Repair

- Inject the existing singleton `IOrganicPluginProtocolCodec` into `PublisherAiBridgeService` through the current DI registration.
- Parse the acknowledged `CapabilityResponse.Properties["Capabilities"]` with `codec.JsonOptions`, matching the existing authenticated TCP connection reader's `TryRead<T>` implementation.
- Extend `tests/static/Assert-OrganicReleaseContracts.ps1` to reject a regression to an ad-hoc JSON options instance on this path.
- Align PublisherStudio Web and Setup project versions at 4.7.8. No installer behavior changed.

No changes to LocalGPT, the 1-Wire contract or package, port discovery, pairing, MFA, approval rules, Council routing, model assignment, or macOS/Linux startup.
