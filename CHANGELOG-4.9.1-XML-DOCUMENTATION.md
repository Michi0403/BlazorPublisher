# PublisherStudio 4.9.1 — XML documentation maintenance

- Remove two stale class-level constructor parameter tags from `AiAssistDeliveryTracker`; retain the real constructor documentation.
- Document `OrganicWorkExecutor.richEditMarkup` and `PublisherAiBridgeService.codec` in their primary-constructor XML comments.
- Update assembly/installer and generated documentation version. No execution behavior or UML-rendering code changed from 4.9.0.

The maintainer's 4.9.0 build reached successful compilation and DocFX generation with four C# documentation warnings. This source-only correction targets those warnings; compilation, PowerShell gates and final rendered UML/PDF verification remain pending on the maintainer environment.
