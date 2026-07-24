# PublisherStudio streaming architecture

PublisherStudio remains document-first. A live production uses the same publication pages, objects, timelines, animations, connectors, data objects, and hotkeys used by editing and export. Streaming adds output contexts and live sources; it does not introduce a second OBS-style scene graph.

## Streaming Studio authoring workflow

Streaming Studio follows the same PublishingSuite/SubApp pattern as the document editors:

- the ribbon chooses the active workspace and exposes commands for the current provider, output, recording plan, LAN setup, device profile, or machine option;
- reusable machine profiles use a navigation pane, while publication-specific settings remain in the main property workspace;
- right-click exposes item-specific commands for provider profiles, outputs, devices, and hotkeys;
- the optional workflow pane explains the safe order of operations without replacing the normal compact form workflow;
- **Apply streaming setup** commits only publication settings, while provider/device/FFmpeg machine options retain their existing explicit save actions.

The interface changes no runtime boundary: publication JSON still stores profile references and output settings, while secrets and machine-specific device information remain outside the publication.

## Runtime layers

1. **PublisherStudio.Web Components** author and render the publication, request browser capture permissions, apply GPU/browser filters, mix browser sources, and create the clean master plus required output canvases.
2. **Controllers/Streaming/UseCases** own the main-host HTTP and WebSocket transport contract for `/api/mediahost`, `/stream`, and `/watch`.
3. **Services/Streaming/UseCases** coordinate runtime information, capture, sessions, Chat, ingest, LAN delivery and editor session control without becoming a new top-level architecture.
4. **Backend/Streaming** owns FFmpeg encoders, native device/process capture, provider Chat, WebRTC/HLS/RTSP/LAN implementation and Now Playing metadata parsing.
5. **HostedServices/Streaming** owns global hotkey lifetime and periodic Twitch OAuth maintenance.
6. **Machine profiles** store reusable provider/device identities outside publication files. Stream keys and Chat OAuth tokens are protected with ASP.NET Core Data Protection; on Windows the key ring is protected with DPAPI.

The solution therefore publishes one `PublisherStudio.Web` application executable. `PublisherStudio.Setup` remains the installation utility, not a required companion runtime. Explicit Company/LAN delivery may open an additional listener inside the same process only when the user enables it.

There is no separately launched Media Host process and no fixed secondary loopback port. The former main-host `StreamingRuntimeEndpoints` aggregation has been removed; application routes are normal MVC controller actions and browser-facing HTTP/WebSocket routes retain the same application origin and paths. A Blazor circuit reconnect is still not the owner of encoder, recording, or LAN-listener lifetime; the main application process is. See `docs/architecture/streaming.md` for component and sequence diagrams.

## One authored page, output-specific Chat

The browser captures one clean base publication frame while authored Chat objects are hidden from that base capture. For every required destination it then:

1. scales the clean base to that output's dimensions;
2. resolves the authored Chat object against the output provider and channel;
3. draws only that provider's messages into the Chat object's published bounds;
4. sends the resulting WebM ingest to the matching integrated output pipeline through same-origin WebSockets.

This keeps all non-Chat pixels synchronized while preventing Twitch messages from appearing in the YouTube program and vice versa. The operator can switch Chat tabs and reply through the same authored Chat component. Broadcast mode does not construct the message composer DOM at all.

A variant canvas is generated only when its provider is enabled or it was selected as a recording variant. Clean-master recording and LAN delivery use the shared clean master.

## Provider Chat adapters

The integrated runtime maintains a separate channel, bounded history, subscriber set, and send path per output.

- **Twitch** uses an authenticated TLS IRC adapter with message tags, timestamps, badges, receive/send, and reconnect.
- **YouTube** uses the authenticated Live Chat HTTP API with polling, page tokens, deduplication, receive/send, and reconnect.
- Other stream providers remain isolated output contexts and can add an adapter without changing the publication or Chat component model.

Stream keys and Chat OAuth credentials are stored separately. A publication refers only to a reusable machine profile ID.

## Twitch OAuth and ingest selection

Twitch profiles can use the existing manual endpoint/key fields or **Twitch web login (OAuth)**. OAuth uses Twitch's Device Code Grant for a public desktop client: PublisherStudio reserves and opens Twitch's activation page, shows the activation code as a fallback, and polls Twitch until the user completes login, consent, and any MFA challenge. Passwords and MFA values never pass through PublisherStudio. Successful completion and explicit cancellation close the reserved window; setup, Client ID, or network failures leave it open with a readable error and Close button.

The Twitch application must be registered by the distributor or user and its public Client ID supplied through one of these locations:

```text
Streaming Studio > Twitch application Client ID
appsettings.json > Twitch:ClientId
PUBLISHERSTUDIO_TWITCH_CLIENT_ID
```

The authorization requests `channel:read:stream_key`; when live Chat is enabled it also requests `chat:read chat:edit` for the existing IRC adapter. The authorized broadcaster ID and login are read from `/validate`, and Helix supplies the channel stream key. Access tokens, rotating refresh tokens, and the stream key are protected with the same machine-profile Data Protection key ring already used by manual credentials.

PublisherStudio validates maintained Twitch sessions immediately when the application starts and every hour thereafter. It refreshes near-expiry/invalid access tokens through the public-client refresh flow and stores the replacement refresh token. Disconnect attempts to revoke the Twitch access token and always removes the local OAuth session. Manual-compatible encrypted stream and Chat secrets remain available when the profile returns to manual mode.

For ingest selection, PublisherStudio downloads Twitch's unauthenticated ingest list, normalizes `{stream_key}` templates, measures two TCP connection attempts to port 1935 from the current computer, and sorts reachable endpoints by measured latency. Twitch Global remains the fallback when direct measurement is blocked or no regional endpoint responds. The user can accept the automatic result or choose any measured endpoint manually.

Other provider profiles remain manual in this revision. OAuth is not exposed as a fake generic switch: YouTube, Kick, TikTok, and custom providers have different application registration, consent, scope, approval, and stream-provisioning requirements and need dedicated adapters.

## Capture and timestamps

Browser APIs remain the first choice for camera, microphone, screen, window, and browser-tab sources. Live sources enter a 48 kHz audio graph with per-source volume and delay and retain device timestamps where the platform exposes them.

The integrated runtime additionally provides:

- Windows DirectShow camera/capture-card discovery and capture;
- macOS AVFoundation discovery and capture;
- Linux V4L2 discovery and capture;
- Windows process-tree WASAPI loopback capture for one application and its child processes;
- FFmpeg-backed network sources such as RTSP, RTMP, SRT, UDP, and TCP.

Native sessions are returned to the publication as bounded WebM streams, so they remain ordinary Publisher objects with the existing crop, transform, filter, animation, connector, and output workflows.

## FFmpeg discovery and installation

Browser camera/webcam, microphone, screen, window, and browser-tab sources use browser media APIs first. FFmpeg is required when PublisherStudio must perform native device capture, isolated application audio, network-protocol ingest, provider encoding, recording, HLS, or RTSP.

PublisherStudio searches an explicitly configured path, the application directory, common package-manager locations, and the operating-system `PATH`. InstallerConsole performs the same check and can provision FFmpeg with WinGet/Chocolatey/Scoop on Windows, Homebrew/MacPorts on macOS, or the available package manager on common Linux distributions. The commands are:

```text
PublisherStudio.Setup --check-ffmpeg
PublisherStudio.Setup --install-ffmpeg
PublisherStudio.Setup --skip-ffmpeg
```

Automatic package-manager installation can require elevation and network access. When provisioning is unavailable, install FFmpeg manually and either expose `ffmpeg` on `PATH` or select its executable in Streaming Studio.

## Filters

Live visual sources share the normal Publisher transform and viewport model and add streaming-oriented filters:

- contain, cover, stretch, crop, opacity, and transforms;
- brightness, contrast, saturation, hue, blur;
- chroma-key color, similarity, smoothing, spill reduction, and residual opacity;
- audio mute, volume, delay, and timestamp preference.

Residual chroma opacity intentionally supports translucent or ghost-like keyed output rather than forcing a fully opaque subject.

## Encoding and quality

Program capture creates a high-bitrate WebM intermediate. Each required output receives its own configured dimensions and frame rate before the integrated runtime performs its final provider/recording encode. FFmpeg hardware encoders are discovered and initialization-tested before NVENC, Quick Sync, AMF, or VideoToolbox is selected; software encoders remain the fallback.

This is currently a decode/re-encode pipeline. It avoids routing raw 4K frames through Blazor and keeps provider processes isolated, but a future native shared-texture/WebCodecs transport can reduce the intermediate generation further.

## Recording

Recording can be started or stopped explicitly during a session. The ribbon separates **Stop streaming** (provider outputs only), **Stop recording** (recording pipelines only), and **Stop session** (complete shutdown). Available recording variants are:

- clean master;
- every enabled output;
- explicitly selected output variants, including a provider version that is not publicly enabled.

Segmented writing limits loss after interruption. FFmpeg input is closed cleanly so container trailers can finalize. Optional MKV-to-MP4 remux copies completed segments without re-encoding.

## Company and LAN output

LAN delivery binds only to the IP address selected by the user and is disabled by default. It can require a session token and enforce a viewer limit.

- **WebRTC** supplies the lowest-latency browser path through same-process PublisherStudio signaling.
- **WebM/MediaSource** is the browser fallback when WebRTC is unavailable or fails.
- **HLS** provides a broadly compatible URL for browsers and VLC.
- **RTSP** provides an RTSP control endpoint with interleaved RTP-over-TCP MPEG-TS delivery for VLC and compatible clients.

The Streaming Studio surfaces the generated browser, HLS, and RTSP addresses rather than requiring the user to construct them.

## Safe defaults and lifecycle

- Dry run never starts public provider delivery.
- Unconfigured profiles do not become active outputs.
- Broadcast Chat never creates operator controls.
- LAN binding is explicit, token protection is available, and listeners are disabled by default.
- Per-output bounded queues prevent one blocked encoder from stalling other outputs.
- Unexpected FFmpeg exits use bounded exponential reconnect while that output remains requested.
- Provider stop disables public outputs while recording and Company/LAN delivery continue.
- Recording stop closes FFmpeg input cleanly and schedules any configured MKV-to-MP4 remux without ending the rest of the session.
- Browser ingest stop requests the final MediaRecorder data, waits for pending WebSocket writes, and closes sockets only after the final chunks are delivered.
- Session stop disposes provider Chat adapters, encoder processes, recordings, native captures, LAN servers, WebRTC peers, ingest subscribers, and global hotkeys.

## Build and hardware verification

The source package includes the main PublisherStudio application, InstallerConsole, and runtime tests. A release build still needs the .NET 10 SDK and licensed DevExpress package feed. FFmpeg is an end-user runtime requirement for native capture, final encoding, recording, and HLS/RTSP delivery; `PublisherStudio.Setup` can detect or install it through an available operating-system package manager. Windows process audio, physical capture devices, real OAuth accounts, and external ingest endpoints must be exercised on the release machine because they cannot be hardware-tested in the source-packaging container.
