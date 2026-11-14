# PublisherStudio 4.7.6 — persistent 1-Wire disk traffic journal

Source revision based on PublisherStudio 4.7.5. Companion LocalGPT source revision: 7.0.5.

## Changes

- Advanced `PublisherStudio.Web` and `PublisherStudio.InstallerConsole` project versions to 4.7.6.
- Kept existing `OrganicWireTrafficJournal` and its in-memory view and ILogger traffic lines. Added an independently persisted **JSON-lines** file on every recorded 1-Wire packet, including TCP/HTTP/UDP discovery when enabled.
- Added live-bound `OrganicPlugins:Traffic` options `FileLoggingEnabled` (default true), `FilePath` (default per-user application-data file), `MaximumFileBytes` (default 64 MiB), with single `.previous` rollover. On Windows the default is `%LOCALAPPDATA%\PublisherStudio\OneWire-traffic.jsonl`.
- The file write uses the same packet-entry payload/frame limits already applied to the in-memory journal. A file error is logged as a warning and does not block packets or the live traffic journal.
- Added `build/Assert-OneWireTrafficFile.ps1` source and configuration guard, invoked from `build/Invoke-ReleaseCompileValidation.ps1`.

## Verification and limitations

- The existing JavaScript test suite passed **36/36** tests under Node.js (`node --test tests/js/*.test.cjs`), including three existing 1-Wire journal contract tests. This does **not** test the new C# file I/O at runtime.
- No PowerShell executable or .NET SDK was available in the editing sandbox. **No .NET compilation, PowerShell source guard execution, or live packet/file persistence run is claimed.** Validate on the Windows/.NET environment before merging.
- Since default payload mode remains `Full` and raw-frame logging remains enabled, protect the disk log directory or set `PayloadMode` to `Redacted`/`None` and disable raw frames when logging potentially sensitive traffic.

## Suggested Windows checks

From the PublisherStudio source root:

```powershell
& ./build/Assert-OneWireTrafficFile.ps1
& ./build/Invoke-ReleaseCompileValidation.ps1 -Solution 'src/PublisherStudio.sln'
```

Use the actual tracked solution path if it differs from the example. The compile-validation script calls dotnet itself; it was not run here.
