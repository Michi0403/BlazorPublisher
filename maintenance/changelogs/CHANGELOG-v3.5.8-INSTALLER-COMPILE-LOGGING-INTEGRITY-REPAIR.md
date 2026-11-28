# PublisherStudio 3.5.8 — installer compile and logging-integrity repair

PublisherStudio 3.5.8 is a focused follow-up to 3.5.7. It keeps the 3.5.7 render-affinity, shared file-sink, GC-pressure, and overlay-installer repairs intact while correcting the compile and maintained-policy regressions that remained in that source package.

- `PublisherStudio.InstallerConsole/Helper/SetupFileLoggerProvider.cs` now explicitly imports both `System` and `System.IO`. The installer project intentionally does not enable implicit usings, so this restores compiler visibility for `Environment`, `DateTimeOffset`, `IDisposable`, `Exception`, `Func<,,>`, `Path`, `Directory`, and `File` without changing installer behavior.
- The shared PublisherStudio file logger remains one provider-owned bounded queue and one writer thread. No per-category file writer, extra queue, or competing file open was reintroduced.
- `FileLogger` restores the accurate `ILogger` compatibility documentation that is part of the maintained logging-integrity baseline.
- `FileLoggerProvider` now has an explicit logged constructor failure boundary around shared-sink creation. This restores the maintained catch-boundary count while improving diagnostics if log-directory/file initialization or writer startup fails.
- The existing logging-integrity baseline was not lowered or refreshed. The repair makes the implementation satisfy the established baseline rather than weakening the guard.
- The existing LocalGPT-aligned `%LOCALAPPDATA%\PublisherStudio` overlay installer contract, one-click setup actions, cross-platform packaging, render modes, localization, and renderer-affinity changes remain unchanged.

No architecture root, public serialization contract, installer deployment dialect, logging provider count, or code style was intentionally changed by this release.
