# PublisherStudio 2.1.7 development diagnostics release

PublisherStudio 2.1.7 keeps the working 2.1.5 publish, installer, DevExpress, DocFX, GitHub Pages, localization, and optional 1-Wire contracts. This release adds observable development exception handling without changing production log volume.

## Development diagnostics

- A DI-owned hosted service subscribes to the framework first-chance exception boundary only in the Development environment.
- PublisherStudio-originated exceptions are logged with their exception object and resolved call site, even when a component later handles them.
- Expected cancellation, disposal, and disconnected-circuit exceptions use Debug level.
- Framework lifecycle `InvalidOperationException` instances use Debug level.
- Unexpected PublisherStudio exceptions use Warning level.
- Repeated call sites receive a bounded number of detailed records followed by interval and shutdown summaries.
- Host cancellation, unexpected termination, and runtime-endpoint cleanup failures have explicit logs.
- No application static logger, static exception registry, or static convenience factory was introduced.

## Release and deployment

The release lane and one-click installer remain unchanged from 2.1.5:

1. publish the exact application and setup runtime assets;
2. preserve their wrapper folders;
3. install beneath `%LOCALAPPDATA%\PublisherStudio`;
4. update matching files without deleting unrelated data;
5. require explicit `--force-delete` before whole-root deletion;
6. maintain Install, Update, Start, and Folder shortcuts.

The LocalGPT wire protocol remains independently pinned to version 2.1.1.
