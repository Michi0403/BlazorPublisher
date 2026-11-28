# PublisherStudio 2.8.3 source changelog

## Build-policy repair

- Fixed the 2.8.2 `Assert-TextServiceOwnership.ps1` failure in `PageSurface.razor`.
- The canvas selection/interaction key no longer performs `string.Join` directly inside the Razor component.
- Added singleton `PublicationEditorTextService`, which owns deterministic canvas selection-key text construction with diagnostics and a safe fallback.
- `PageSurface` now delegates selection-key construction to that service while preserving the exact interaction-suspension semantics introduced in 2.8.2.

## Regression protection

- Preserved Picture Studio / overlay mainframe interaction suspension without z-index or layer-stack restructuring.
- Preserved browser-native range controls with coalesced InteractiveServer input delivery.
- Preserved Media Converter Studio smart editable FFmpeg guidance.
- Preserved Video Studio rendered-video export and the existing editable layer/effect graph.
- Preserved the five reviewed InteractiveServer boundaries and LocalGPT 1-Wire protocol package version 2.1.1.

## Version

- PublisherStudio Web: 2.8.3
- PublisherStudio Installer Console: 2.8.3
- Active browser cache tokens rolled to 2.8.3 / 20260818-283.
