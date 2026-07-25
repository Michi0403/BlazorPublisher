# PublisherStudio v1.0.79 changelog

## Creator Hub platform-chat compile and architecture fix

- Fixed `CS0117` in `PanelDocumentService.cs`: `PlatformChat` was referenced as a `PublicationLiveSourceKind` even though platform chat is not a browser/native capture source.
- Preserved and completed the intended feature by building the Creator / Gamer Hub chat area with PublisherStudio's shared `PublicationComponentKind.Chat` component.
- The preset chat now uses `PublicationChatPlatform.OutputContext`, so preview, Twitch, YouTube, channel isolation, sending, avatars and timestamps continue through the existing streaming/chat runtime.
- Added shared-component normalization to recursive Panel/Div documents. Chat and every other `DevExtremeComponentElement` nested inside panels now use the same normalization contract as page-level components.
- Added regression coverage that rejects unknown `PublicationLiveSourceKind` member references across C# and Razor sources before they become `CS0117` compiler failures.
- Added Creator Hub contract coverage that requires the shared chat component and forbids modeling platform chat as a capture source.

## Compatibility

- Publication format remains `1.54`; no document migration is required.
- Package dependencies and FFmpeg integration behavior are unchanged.
- Application, installer, npm, lock-file, streaming runtime and structured-export manifest versions are `1.0.79`.
