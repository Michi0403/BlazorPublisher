# PublisherStudio v1.0.54 release

See `CHANGELOG-v1.0.54.md`, `CHANGELOG-v1.0.53.md`, `docs/STREAMING.md`, and `VALIDATION.md`.

This release hardens the setup path after FFmpeg provisioning exposed slow or intermittent GitHub/CDN behavior. PublisherStudio release assets can resume from `.part` files, cached complete ZIPs are validated and reused, metadata and asset transfers retry, stalled reads are bounded, and both payloads are staged before the current installation is touched.

On Windows, FFmpeg provisioning now uses only the WinGet community source for `Gyan.FFmpeg`, avoiding unrelated Microsoft Store source failures. Package-manager execution has heartbeat output, process-tree termination, retry support, executable verification, and a 15-minute total budget. Failure to provision optional FFmpeg does not invalidate the application installation.

The v1.0.53 Twitch OAuth/ingest implementation and v1.0.52 standalone tooltip-coordinate correction remain included unchanged.

Application and installer version `1.0.54`; publication format `1.45`; picture format `1.2`. There is no separate Media Host executable or release payload.
