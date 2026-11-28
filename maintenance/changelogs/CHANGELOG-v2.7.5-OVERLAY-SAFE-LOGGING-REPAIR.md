# PublisherStudio 2.7.5 — Overlay-safe logging repair

- Repairs in-place upgrades where the transient 2.7.4 `Logging/FileLogger*.cs` location and the older `Services/Logging/FileLogger*.cs` location could coexist.
- Restores the canonical file logger implementation under `Services/Logging` and keeps logger options/state under `BusinessObjects`, matching PublisherStudio's maintained Service/Controller/BusinessObject architecture.
- Ships declaration-free tombstones at the transient 2.7.4 `Logging` paths and excludes those paths from compilation so source updates over an existing tree cannot produce duplicate or stale logger implementations.
- Uses the same runtime-directory fallback behavior as LocalGPT: an empty configured file path writes `PublisherStudio.log` in `Directory.GetCurrentDirectory()`; the installer starts the executable with its runtime directory as the working directory.
- Aligns the file logger with the actual current business objects. It no longer expects removed `ExceptionType`, `ExceptionMessage`, `ExceptionStackTrace`, `ResolvePath`, or `MaxQueueLength` members.
- Adds the canonical logger implementation to the reviewed logging-integrity baseline instead of requiring a logger provider to recursively log through itself.
- Completes XML documentation on the canonical logging implementation so the maintained documentation validator covers the real compile path.
- Preserves the 2.7.3 media-recording finalization and reconnect recovery behavior without additional media changes.
- LocalGPT and the LocalGPT wire protocol are unchanged.
