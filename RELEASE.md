# PublisherStudio 3.4.8

PublisherStudio 3.4.8 keeps the 3.4.6 FFmpeg/path-discovery repair and adds one application-owned storage contract. Mutable configuration, runtime state and default Publisher content remain per-user by default; portable and system-wide locations remain discovery/install or explicit-override candidates.

Windows keeps the established `%LOCALAPPDATA%\PublisherStudio` default. macOS and Linux use their normal per-user application-data roots, with Linux honoring `XDG_DATA_HOME`. First boot records the effective layout for in-app troubleshooting.

See `CHANGELOG-v3.4.8-PATH-LAYOUT-XML-DOCUMENTATION-REPAIR.md` and `VALIDATION-v3.4.8-source.md`.
