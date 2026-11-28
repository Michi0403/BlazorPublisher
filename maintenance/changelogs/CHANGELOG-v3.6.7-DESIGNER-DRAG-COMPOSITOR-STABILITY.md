# PublisherStudio 3.6.7 — designer drag and compositor stability

## Embedded object movement

- Deferred server-side selection mutation for movable publication objects until pointer-up or final drag commit. The editor still applies optimistic selection immediately in the DOM, so selection feedback remains instant without allowing a Blazor rerender to replace the dragged object's DOM node before movement begins.
- Drag commits remain authoritative through `CommitMove`, including multi-selection and grouped movement. Locked objects and connectors keep immediate selection behavior because they do not enter the movable pointer operation.
- HTML embed and Panel elements now use the transform-compatible zoom strategy in the Chromium/Edge designer. This keeps iframe/canvas/composited content visually attached to the same outer publication element that owns its selection frame during live movement.

## Browser scheduling

- Replaced the main canvas's unconditional gamepad animation-frame loop with a demand-driven loop that runs only with an active/focused canvas and a connected gamepad. Pointer, keyboard-focus and gamepad-connection transitions reactivate it, so controller/Steam Deck editing remains available without a permanent idle loop.
- Applied the same policy to Panel Studio: polling stops when hidden, unfocused, inactive or disconnected and resumes from focus, pointer, visibility and gamepad connection events.
- All new listeners are tied to existing disposal/AbortController paths.

These changes reduce needless Chromium compositor activity and are defensive against the same class of browser/WindowServer pressure observed while testing LocalGPT; PublisherStudio does not contain LocalGPT's provider/TLS paths.

## Preserved behavior

- Media controls, DevExtreme native component interaction, connector editing, crop/content-pan and multi-selection remain on their existing specialized paths.
- The metadata-backed application/setup console identity and batched macOS package inspection from prior releases remain unchanged.
