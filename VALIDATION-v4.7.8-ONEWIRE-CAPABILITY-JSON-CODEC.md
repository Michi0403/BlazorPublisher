# PublisherStudio 4.7.8 — 1-Wire capability JSON acceptance

## Static verification

- The source bridge injects `IOrganicPluginProtocolCodec` and parses the received `Capabilities` JSON element using `codec.JsonOptions`, with the same options already used by `LocalGptConnectionService.TryRead<T>`.
- The existing `OrganicPluginProtocolCodec` includes `JsonStringEnumConverter`; its DI registration is singleton.
- `tests/static/Assert-OrganicReleaseContracts.ps1` checks the codec dependency and rejects an ad-hoc `JsonSerializerOptions` in the capability response path.
- Both PublisherStudio project versions are 4.7.8; all original ZIP paths are preserved.

## Manual acceptance on user's machines

1. Run `pwsh ./tests/static/Assert-OrganicReleaseContracts.ps1` and repository preflights (not executed in the packaging environment).
2. Build PublisherStudio 4.7.8 with the retained wire package 2.1.1. No LocalGPT rebuild needed for this change.
3. Keep LocalGPT paired, invoke the embedded PublisherStudio chat with the current large capability directory, and confirm no `$[0].InteractionEditor` `JsonException`.
4. Verify the log now contains `CapabilityRequest`, `CapabilityResponse`, then `CouncilRequest` when `council.run` is enabled/online.
5. Exercise a LocalGPT installation with an available configured AI model; verify Council completion. With no reachable model, the corrected bridge may return a truthful model-selection error rather than a successful answer.
6. Repeat macOS ARM64 and Windows with the same capability contracts, and test a failed WorkResult to confirm errors are reported accurately.

## Limits

Source ZIP/static checks only. This environment did not run PowerShell, a .NET build, real 1-Wire pairing, a browser component, or platform-specific runtime verification. Do not regard runtime behavior as proven until tested.
