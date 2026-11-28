# PublisherStudio 3.4.7 — per-user data and first-boot path contract

PublisherStudio 3.4.7 centralizes mutable application storage while preserving the long-established Windows `%LOCALAPPDATA%\PublisherStudio` default.

- Per-user writable state is the default on Windows, macOS and Linux.
- Windows keeps `%LOCALAPPDATA%\PublisherStudio`.
- macOS uses the current user Application Support location.
- Linux honors `XDG_DATA_HOME` when supplied and otherwise uses the normal `~/.local/share` application-data base. This applies to Fedora, Debian/Ubuntu, Arch, SteamOS and other standard desktop Linux distributions.
- Publisher content defaults (projects, images, video, audio, documents, exports and OpenSCAD work) remain under the PublisherStudio user root unless a project/configuration override explicitly selects another location.
- User configuration is loaded from `Configuration/appsettings.user.json`; environment variables remain higher-precedence overrides.
- Data Protection, spreadsheet hibernation, runtime endpoint state, streaming state, media conversion, recovery, localization/user catalogs, templates and Organic state use the canonical per-user root.
- FFmpeg/tool discovery remains separate from PublisherStudio mutable storage, retaining configured, portable, user package-manager and system-wide candidates.
- First boot creates the effective directory structure and writes `Configuration/path-layout.json`; the Help page and `/api/configuration/path-layout` expose the detected layout for troubleshooting.

System-wide paths and the portable application directory are discovery/install candidates only and do not silently become the default mutable storage scope.
