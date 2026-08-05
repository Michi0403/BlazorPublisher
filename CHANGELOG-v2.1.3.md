# PublisherStudio 2.1.3

- Fixed `PublisherStudio.InstallerConsole` compilation by assigning the detached `Process.Start` result.
- Removed the orphaned XML documentation block in `WebDataController`.
- Reworked `Build-Release.ps1` around the LocalGPT-shaped shared `all` runtime lane.
- Generates and validates one DocFX/PDF payload, then reuses it for every runtime package.
- Replaced the older release-download Pages workflow with LocalGPT's pinned Kawaii snapshot workflow.
- Replaced the forked documentation theme with the current LocalGPT Kawaii implementation using PublisherStudio identifiers.
- Added the persistent Auto/Light/Dark selector, cat-paw favicon/logo, and matching validation markers.
- Added a validated PublisherStudio Pages snapshot and a one-click snapshot refresh command.
- Kept PublisherStudio-specific DevExpress, FFmpeg, and optional 1-Wire steps as the smaller product subset.

- Retains the Application language selection and JSON-backed culture flow introduced in 2.1.2.
- Build-policy repair retains the restored logging-integrity and architecture gates without weakening them.
