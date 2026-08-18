# PublisherStudio 2.9.0 source validation

This is a source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, pack, EF migration, or database command was executed while preparing this archive.

Validated statically and with non-.NET tooling:

- PublisherStudio Web and InstallerConsole versions are 2.9.0;
- the user's DevExpress 25.2.9 and .NET 10 / 10.0.11 upgrade changes remain present;
- recovery debounce asynchronous work no longer accepts or dereferences a `CancellationTokenSource` after scheduling;
- a source scan found zero async C#/Razor methods accepting `CancellationTokenSource` directly after the repair;
- ordinary debounce replacement uses a non-throwing cancellation signal rather than cancellation-backed `Task.Delay`;
- editor disposal cancels the active recovery owner without disposing the source out from under the supervised worker;
- Media Converter Studio polling captures its token before scheduling, worker-owns cancellation-source disposal, and no longer uses cancellation-backed `Task.Delay` for ordinary polling shutdown;
- media-conversion FFmpeg jobs capture their execution token before supervision, and `Remove` / service disposal no longer dispose a cancellation source while the worker can still be running;
- `mediaStudioInterop.js` contains the WebM EBML duration repair and uses it only for the embedded transfer copy;
- the original retained MediaRecorder Blob remains the source for Download recording;
- the actual new JavaScript helper was executed with Node against the supplied 3828x1962 VP9/Opus WebM and produced a 14.800-second duration recognized by `ffprobe`;
- `node --check` passes for the changed maintained JavaScript;
- JavaScript diagnostics manifest hashes are refreshed with normalized-newline SHA-256;
- InteractiveServer/prerender boundaries, 1-Wire 2.1.1, XML documentation enforcement, component/service resilience, text-service ownership, and ConfigureAwait policy remain in the source tree;
- generated build/cache directories are excluded from the returned archive.
