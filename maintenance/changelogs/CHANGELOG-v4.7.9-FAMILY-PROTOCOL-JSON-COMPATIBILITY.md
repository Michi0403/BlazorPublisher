# PublisherStudio 4.7.9 — family protocol JSON compatibility

- `OrganicPluginProtocolCodec` now creates its one-wire serialization settings through `FamilyWireJsonConventions`, matching LocalGPT's equivalent policy for property input, omitted nulls and string-enum values. The codec also reads quoted numeric values emitted by LocalGPT MVC without writing quoted numbers back onto the signed one-wire transport.
- `PublisherAiBridgeService` continues to use the protocol codec to parse live capabilities and now also uses it when serializing the project context inside one-wire Council requests.
- MVC HTTP APIs now use the family-compatible string-enum converter and case-insensitive JSON input, while keeping the **existing MVC camel-case output naming, numeric writing rules, and reference handling**. The string-enum output change for enum-containing MVC responses must be checked against existing consumers; legacy integer enum **input** remains supported.
- The existing one-wire HTTP profile endpoint keeps its MVC response formatting, so it is not arbitrarily switched to the PascalCase envelope formatter.
- Added a PowerShell source-contract preflight invoked by the existing compatibility guard. The guard checks for accidental JSON codec bypasses and preserved API wiring.

## SignalR and MessagePack boundary

PublisherStudio's streaming `PlatformChatHub` and `WebRtcSignalingHub` are plain registered application services; the project has no separately mapped ASP.NET Core SignalR hub route. The Blazor Server UI uses framework-managed SignalR. It is intentionally not reconfigured with LocalGPT's application hub settings: doing so would change an unrelated internal client protocol. PublisherStudio has no custom MessagePack SignalR transport to harmonize.

## Nonchanges and checks

- No changes to installers or `LocalGPT.WireProtocolVersion` dependency, 1-Wire pairing/MFA/security, component automation, inference, or local data paths.
- Build on Windows/macOS/Linux and run app-to-app 1-Wire capability, Council, approval, result and HTTP/JSON round trips.
- Test all enum-bearing PublisherStudio MVC responses with existing JS/.NET clients, including any clients expecting numeric enum **output**; the new output uses enum names.
- Check Blazor interactive circuit and DevExpress components after boot, then LocalGPT MCP and SignalR separately.

**Evidence:** static source checks only; no .NET build, PowerShell execution, runtime compatibility or installer test was performed in the preparation environment.
