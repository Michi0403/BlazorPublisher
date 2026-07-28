# PublisherStudio 2.0.1 final27 - compiler and maintainer manifest fix

- Added the missing instance logger field and constructor assignment in `PanelStudioTextPatternDataService`, resolving the four `CS0103` errors without making the helper methods static or moving pattern values out of object storage.
- Added a reviewed PowerShell manifest refresher for local maintainer work. It supports exact file review and a confirmed current-change mode.
- The refresher runs security, 1-Wire, runtime-value ownership, JavaScript diagnostics, and protected-architecture safeguards and rolls manifests back on post-write failure.
- final19 security hashes and removal-only baselines cannot be refreshed by the new tool.
