# PublisherStudio v1.0.50 release

See `CHANGELOG-v1.0.50.md`, `docs/STREAMING.md`, `docs/COMPONENT_RUNTIME.md`, and `VALIDATION.md`.

This revision restores the single-application architecture: streaming, native capture, FFmpeg orchestration, recording, provider delivery, Chat, hotkeys, and same-origin browser/WebSocket routes now run inside `PublisherStudio.Web`. It also corrects context-menu coordinates, makes tooltip stacking overlay-aware, adds live-source double-click/context commands, and adds cross-platform FFmpeg detection/provisioning to InstallerConsole.

The source package intentionally excludes `bin`, `obj`, `.vs`, `.git`, restored `node_modules`, local credentials, recordings, and generated runtime caches.

Application and installer version `1.0.50`; publication format `1.45`; picture format `1.2`. There is no separate Media Host executable or release payload.
