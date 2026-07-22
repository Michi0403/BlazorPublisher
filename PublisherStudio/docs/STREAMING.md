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

1. **PublisherStudio.Web** authors and renders the publication, requests browser capture permissions, applies GPU/browser filters, mixes browser sources, and creates the clean master plus required output canvases.
2. **PublisherStudio.MediaHost** owns the long-running session, native capture, FFmpeg encoders, recordings, provider delivery, LAN endpoints, provider Chat connections, Now Playing metadata, and Windows global hotkeys.
3. **Machine profiles** store reusable provider/device identities outside publication files. Stream keys and Chat OAuth tokens are protected with ASP.NET Core Data Protection; on Windows the key ring is protected with DPAPI.

The Media Host is deliberately separate from the Blazor circuit. A browser reconnect is not the owner of the encoder, recording, or LAN listener lifetime.

## One authored page, output-specific Chat

The browser captures one clean base publication frame while authored Chat objects are hidden from that base capture. For every required destination it then:

1. scales the clean base to that output's dimensions;
2. resolves the authored Chat object against the output provider and channel;
3. draws only that provider's messages into the Chat object's published bounds;
4. sends the resulting WebM ingest to the matching Media Host output pipeline.

This keeps all non-Chat pixels synchronized while preventing Twitch messages from appearing in the YouTube program and vice versa. The operator can switch Chat tabs and reply through the same authored Chat component. Broadcast mode does not construct the message composer DOM at all.

A variant canvas is generated only when its provider is enabled or it was selected as a recording variant. Clean-master recording and LAN delivery use the shared clean master.

## Provider Chat adapters

The Media Host maintains a separate channel, bounded history, subscriber set, and send path per output.

- **Twitch** uses an authenticated TLS IRC adapter with message tags, timestamps, badges, receive/send, and reconnect.
- **YouTube** uses the authenticated Live Chat HTTP API with polling, page tokens, deduplication, receive/send, and reconnect.
- Other stream providers remain isolated output contexts and can add an adapter without changing the publication or Chat component model.

Stream keys and Chat OAuth credentials are stored separately. A publication refers only to a reusable machine profile ID.

## Capture and timestamps

Browser APIs remain the first choice for camera, microphone, screen, window, and browser-tab sources. Live sources enter a 48 kHz audio graph with per-source volume and delay and retain device timestamps where the platform exposes them.

The Media Host additionally provides:

- Windows DirectShow camera/capture-card discovery and capture;
- macOS AVFoundation discovery and capture;
- Linux V4L2 discovery and capture;
- Windows process-tree WASAPI loopback capture for one application and its child processes;
- FFmpeg-backed network sources such as RTSP, RTMP, SRT, UDP, and TCP.

Native sessions are returned to the publication as bounded WebM streams, so they remain ordinary Publisher objects with the existing crop, transform, filter, animation, connector, and output workflows.

## Filters

Live visual sources share the normal Publisher transform and viewport model and add streaming-oriented filters:

- contain, cover, stretch, crop, opacity, and transforms;
- brightness, contrast, saturation, hue, blur;
- chroma-key color, similarity, smoothing, spill reduction, and residual opacity;
- audio mute, volume, delay, and timestamp preference.

Residual chroma opacity intentionally supports translucent or ghost-like keyed output rather than forcing a fully opaque subject.

## Encoding and quality

Program capture creates a high-bitrate WebM intermediate. Each required output receives its own configured dimensions and frame rate before the Media Host performs its final provider/recording encode. FFmpeg hardware encoders are discovered and initialization-tested before NVENC, Quick Sync, AMF, or VideoToolbox is selected; software encoders remain the fallback.

This is currently a decode/re-encode pipeline. It avoids routing raw 4K frames through Blazor and keeps provider processes isolated, but a future native shared-texture/WebCodecs transport can reduce the intermediate generation further.

## Recording

Recording can be disabled or toggled during a session. Available variants are:

- clean master;
- every enabled output;
- explicitly selected output variants, including a provider version that is not publicly enabled.

Segmented writing limits loss after interruption. FFmpeg input is closed cleanly so container trailers can finalize. Optional MKV-to-MP4 remux copies completed segments without re-encoding.

## Company and LAN output

LAN delivery binds only to the IP address selected by the user and is disabled by default. It can require a session token and enforce a viewer limit.

- **WebRTC** supplies the lowest-latency browser path through Media Host signaling.
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
- Session stop disposes provider Chat adapters, encoder processes, recordings, native captures, LAN servers, WebRTC peers, ingest subscribers, and global hotkeys.

## Build and hardware verification

The source package includes all implementation projects and runtime tests. A release build still needs the .NET 10 SDK, the licensed DevExpress package feed, FFmpeg, and target-platform hardware/provider credentials. Windows process audio, physical capture devices, real OAuth accounts, and external ingest endpoints must be exercised on the release machine because they cannot be hardware-tested in the source-packaging container.
