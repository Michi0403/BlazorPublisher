# PublisherStudio 3.5.6

PublisherStudio 3.5.6 is a release-path recovery build. It keeps `%LOCALAPPDATA%\PublisherStudio` as the canonical Windows root, restores durable setup/application logging, stages and transactionally replaces Windows runtime/setup wrappers, validates matching incoming release identities without blocking upgrades from an older setup, and falls back from an unavailable Windows loopback port while retaining 58071 as the preferred default.

It also preserves the 3.5.3 frontend race, export-slider, Gallery multi-picture and cache-identity repairs; the 3.5.3/3.5.4 macOS distribution-PKG validation/readback; the server-rendezvous model; PowerShell 5.1 documentation compatibility; and existing render/architecture boundaries.

See `CHANGELOG-v3.5.6-INSTALLER-LOGGING-PORT-RECOVERY.md` and `VALIDATION-v3.5.6-source.md`.
