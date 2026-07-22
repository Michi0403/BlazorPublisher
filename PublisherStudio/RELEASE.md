# PublisherStudio v1.0.53 release

See `CHANGELOG-v1.0.53.md`, `docs/ANIMATION_EXPORT.md`, `docs/COMPONENT_RUNTIME.md`, `docs/STREAMING.md`, and `VALIDATION.md`.

This revision adds official Twitch OAuth Device Code Grant authorization, encrypted token/stream-key persistence, startup and hourly token validation, refresh-token rotation, OAuth disconnect/revocation, and automatic Twitch ingest selection based on measurements from the local computer. The existing manual Twitch configuration remains available, and non-Twitch provider profiles retain their manual workflows.

The v1.0.52 standalone DevExtreme tooltip-coordinate fix remains included unchanged.

The source package intentionally excludes `bin`, `obj`, `.vs`, `.git`, restored `node_modules`, local credentials, recordings, and generated runtime caches.

Application and installer version `1.0.53`; publication format `1.45`; picture format `1.2`. There is no separate Media Host executable or release payload.
