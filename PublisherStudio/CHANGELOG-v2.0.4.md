# PublisherStudio 2.0.4 — service-owned compiler repair

PublisherStudio 2.0.4 repairs the compiler layer exposed after the 2.0.3 Windows build-policy fixes. The repair follows the current LocalGPT architecture direction: application behavior is instance/service-owned, data contracts live under `PublisherStudio.BusinessObjects`, and no new application statics were introduced.

## Compiler and composition repair

- Startup now passes an explicit bootstrap logger into `AddPublisherStudioApplication`.
- Blazor imports expose `RenderMode.InteractiveServer` exactly once, resolving generated Razor errors without adding page-local aliases.
- Former static document helpers are replaced by the existing injected `IPublisherDocumentFactory`.
- `PublicationComponentService`, page-preset, story-layout, file-name and streaming-result calls now use injected services.
- Native capture, process loopback, platform Chat and LAN streaming runtime objects are created through DI-owned factories.
- Constructor drift in `NativeCaptureSession`, `WindowsProcessLoopbackCapture`, `PlatformChatService` and `LanStreamingServer` is removed.
- The native-capture logger-factory self-assignment bug is removed.

## Business-object boundary

- Data contracts moved from the former `PublisherStudio.Domain` namespace to `PublisherStudio.BusinessObjects`.
- Runtime behavior formerly mixed into model files moved to services, including RichEdit document creation, publication-media normalization, geometry behavior and traversal behavior.
- `PagePreset`, `StoryPageLayout`, `StreamingChatSendResult`, `PlatformChatMessage`, `PublicationPoint` and `WordArtPathPoint` are business objects.
- Page presets and story-layout defaults are configuration-backed and resolved by services.

## Publish guard repair

- `Assert-PublishConfiguration.ps1` now parses each runtime mapping block and validates `AppFolder`, `SetupFolder` and `SetupAsset` independently.
- The guard no longer relies on fragile PowerShell array/string concatenation behavior that reported an existing mapping as missing.

## Compatibility

The preservation-first 2.0.2 installer behavior and 2.0.3 build-policy repairs remain intact. LocalGPT wire protocol compatibility stays pinned independently at 2.1.1.
