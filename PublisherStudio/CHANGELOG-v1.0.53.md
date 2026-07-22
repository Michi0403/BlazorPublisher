# PublisherStudio 1.0.53

## Twitch OAuth and automatic ingest selection

- Added Twitch OAuth Device Code Grant support for public desktop clients. The user completes login, consent, password, and MFA on Twitch; PublisherStudio never receives those credentials.
- Twitch OAuth profiles request `channel:read:stream_key` and optionally `chat:read chat:edit`, resolve the authorized broadcaster identity, retrieve the stream key through Helix, and store access token, rotating refresh token, and stream key with the existing operating-system protected machine profile store.
- OAuth sessions are validated at application startup and hourly. Expiring or invalid access tokens are refreshed through Twitch's rotating public-client refresh-token flow, while disconnect revokes the current token when Twitch is reachable and always removes the local OAuth session.
- Changing a Twitch Client ID invalidates the stored OAuth session instead of silently reusing tokens issued to another application.
- Manual Twitch endpoint, account, stream-key, and Chat-token configuration remains available. Disconnecting OAuth returns the profile to manual mode without deleting its encrypted manual-compatible stream key or Chat token.
- Added an official Twitch ingest-list refresh using `https://ingest.twitch.tv/ingests`. PublisherStudio measures two TCP connection samples per endpoint from the local machine, selects the lowest reachable latency, and retains Twitch Global as the fallback.
- Twitch endpoint templates accept both `{stream_key}` and `{streamKey}` placeholders.

## Interface and configuration

- Streaming Studio now offers **Manual stream key** and **Twitch web login (OAuth)** connection methods.
- Added browser authorization window handling, activation-code fallback, OAuth status/scopes, endpoint testing, measured endpoint selection, reconnect, cancel, and disconnect controls.
- A public Twitch application Client ID can be supplied in the profile, `Twitch:ClientId` in `appsettings.json`, or `PUBLISHERSTUDIO_TWITCH_CLIENT_ID`.
- Other providers keep their existing manual profiles. Their OAuth implementations are intentionally not simulated because each provider requires its own registered application, scopes, approval rules, and stream-creation API.

## Compatibility and validation

- Existing provider profile protection purpose remains unchanged, so v1 streaming secrets stay readable.
- Publication JSON remains `1.45`; Picture Studio format remains `1.2`.
- Existing streaming outputs, per-output Chat, FFmpeg orchestration, recordings, LAN delivery, device capture, hotkeys, exports, and tooltip fixes remain wired as before.
- Project JavaScript syntax and all Node contract suites pass. A full .NET build still requires the .NET 10 SDK and licensed DevExpress package feed.
