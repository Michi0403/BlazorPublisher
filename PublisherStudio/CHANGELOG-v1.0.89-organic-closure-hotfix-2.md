# PublisherStudio v1.0.89 organic source-closure hotfix 2

## Build regression fixed

- Restored `Services/Streaming/UseCases/Runtime/StreamingRuntimeUseCases.cs` to the delivered source tree. Its namespace matches `GlobalUsings.Streaming.cs`, `StreamingRuntimeController`, `StreamingMediaHostClient` and `StreamingServiceCollectionExtensions`.
- Restored the complete `src/LocalGPT.WireProtocolVersion` project referenced by `PublisherStudio.Web.csproj`.
- Added a source-closure test that resolves every `<ProjectReference>`, requires the streaming runtime use-case source and validates the shared protocol project before packaging.
- Added `New-VerifiedSourcePackage.ps1`, which runs the closure and organic-protocol tests and packages every maintained source file while excluding generated folders.

## Organic protocol and workflow included

- The PublisherStudio protocol project is a synchronized offline-build mirror of the authoritative LocalGPT `LocalGPT.WireProtocolVersion` project, not a competing protocol design.
- Protocol v1.3 exposes bidirectional human/automated target-system interaction requirements plus `InteractionValueJson` and content type.
- Capability, skill and UI-feature state negotiation can hide, disable or enable frontend functions based on current connection and advertised online/enabled state.
- Recurring screen-reader sessions enforce a minimum 15-second interval, single active screenshot per session, busy-tick skipping and bounded latest-result handling instead of stacking work.
- CPU/GPU/accelerator and model token-route descriptors are exchanged through the shared protocol.
- OpenSCAD, spreadsheet, screenshot/input, text-proposal and media capabilities continue to reuse the existing PublisherStudio services and object models.
- The existing organic approval bar/page and permission matrix remain the human gate for consequential eye/hand work.

## Validation performed here

- The complete `npm test` suite passed, including installer resilience, architecture, C# source-compilation safety, SDK default items, OpenSCAD, automation/screenshots, streaming and organic-plugin tests.
- All project references resolve in the packaged source tree and `StreamingRuntimeUseCases.cs` is explicitly guarded against future omission.
- Native .NET 10/Razor/DevExpress compilation remains for the owner workstation as requested.

## Missing features / next bounded run

See `MISSING_FEATURES-v1.0.89-organic-closure-hotfix-2.md`. Deferred items are not represented as completed.
