# PublisherStudio v1.0.89 — Organic Learning Hotfix 3

## Build repair

- Fixed `CS1061` in `OrganicCapabilityAndExecutionServices`: `MediaConversionCapabilities` exposes `Available`, not `IsAvailable`.
- Keeps `StreamingRuntimeUseCases.cs` and the complete shared `LocalGPT.WireProtocolVersion` project in the source package.
- Extends source-package tests so the removed-class/project regression fails before delivery.

## LocalGPT text proposal workflow

- Adds a visible PublisherStudio form for starting a selected LocalGPT Council/ Learning Round to generate a reviewable text proposal.
- Requests the LocalGPT function `publisher.text.proposal.request`, which returns through PublisherStudio's existing `publisher.text.insert.propose` capability.
- Text remains a proposal in the Council results area; no publication content is changed automatically.
- Adds `learning-round` to the negotiated Council team selector and uses large local context/output request defaults.

## Organic plugin behavior retained

- Existing discovery, permissions, approval, screen capture, recurring screen-reader, browser input, OpenSCAD, spreadsheet and FFmpeg/media capability paths remain intact.
- PublisherStudio still applies its per-peer/capability/organ permission rule after LocalGPT approval.
- Recurring screen-reader help remains single-flight and interval-clamped to prevent stacking/races.

## Validation

- The full PublisherStudio `npm test` suite passes, including source closure, C# compilation-safety heuristics, installer resilience, streaming, OpenSCAD, automation and organic-plugin contracts.
- Organic-plugin tests now reject `media.IsAvailable` and verify Learning Round/direct-text wiring.
- See `MISSING_FEATURES-v1.0.89-organic-learning-hotfix-3.md` for remaining work and native-build limits.
