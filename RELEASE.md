# PublisherStudio 2.9.0

PublisherStudio 2.9.0 starts from the user's locally building 2.8.9 .NET/DevExpress-upgraded source and repairs two runtime problems found during Video Studio testing: recovery-debounce cancellation lifetime races and browser-recorded WebM insertion with missing container duration metadata.

## Toolchain state retained

- Target framework: `net10.0`
- DevExpress: `25.2.9`
- dotnet-ef tool: `10.0.11`
- Installer `Microsoft.Extensions.Logging`: `10.0.11`
- SDK policy: `10.0.301` minimum with `latestFeature` roll-forward
- LocalGPT 1-Wire protocol: `2.1.1`

## Runtime repairs

- Recovery debounce workers now receive captured `CancellationToken` values while their owning `CancellationTokenSource` remains alive until the worker finishes. Normal debounce replacement uses a non-throwing cancellation signal instead of cancellation-backed `Task.Delay`.
- Browser MediaRecorder WebM files are duration-repaired only for the publication-embedded copy. The original retained browser Blob remains untouched for direct download.
- The repair reads WebM `TimecodeScale` and writes/updates the Matroska `Duration` element before the existing chunked embed transfer, covering both video/webm and audio/webm recordings.

See `CHANGELOG-v2.9.0-RECOVERY-CANCELLATION-WEBM-EMBED-REPAIR.md` and `VALIDATION-v2.9.0-source.md`.
