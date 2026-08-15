# PublisherStudio 2.7.3 — Logging and recording recovery

## Logging restored

- Restored the `LoggingCore` application logging contract using the same architecture concepts as LocalGPT: logging options BusinessObjects, logging BusinessObjects plus DI-owned logger/provider services under `Services/Logging`, composed through `LoggingConfigurationService`.
- Default file output is `%LocalAppData%\PublisherStudio\PublisherStudio.log`; the path remains configurable through `LoggingCore:FileCore:FilePath`.
- Information-level PublisherStudio application diagnostics remain available outside Development while noisy Microsoft/System framework categories stay warning-filtered.
- Closed the remaining iterator resilience gaps: `yield` service methods now use `try/finally` plus diagnostics. Normal service methods continue to be required to own `try/catch` plus diagnostics.

- Removed legacy `static` declarations from Razor component helpers; component behavior remains instance-scoped and the architecture audit now covers Razor declarations too.

## Video/Audio Studio recording

- `Stop recording` now waits for MediaRecorder's final data chunk and retained browser Blob instead of immediately stopping capture tracks.
- The browser-owned completed recording remains authoritative even if the Blazor circuit disconnects while the `.NET` callback is being delivered.
- Media Studio recovers active/retained recording state after a reconnect and restores the completed file into the editor.
- Stop/start/finalization/download failures now produce structured PublisherStudio log events.

## Preserved

- Existing Video Studio drag/drop, Picture Studio behavior, editor workflows and LocalGPT bridge behavior are intentionally unchanged.
- LocalGPT Wire Protocol package remains 2.1.1.
- No `@rendermode` directive is intentionally changed.
