# PublisherStudio 3.2.5 source validation

Static validation only; no .NET build was run.

- Confirmed PublisherStudio application and installer-console versions are 3.2.5.
- Confirmed the generated macOS launcher has an immediate visible Terminal log console by default, creates the log before tailing it, retains HTTP/runtime-endpoint readiness probes, and supports `PUBLISHERSTUDIO_SHOW_CONSOLE=0`.
- Confirmed the macOS launcher write-probes only PublisherStudio-owned per-user Application Support/runtime/Logs/Caches directories and scopes any administrator ownership repair to the failing user directory rather than `/Applications/PublisherStudio.app`.
- Confirmed new 1-Wire secret storage prefers LocalApplicationData while preserving an existing writable portable secret.
- Confirmed `ApplicationPathService` uses per-user application-data fallbacks when OS known folders are unavailable instead of falling back to `AppContext.BaseDirectory`.
- Confirmed Linux AppImage desktop metadata uses `Terminal=true` and the AppRun wrapper checks writable XDG data/state/cache directories.
- Confirmed the generated macOS launcher and Linux AppRun shell bodies pass `sh -n` after placeholder substitution.
- Confirmed 3.2.4 staging cleanup plus 3.2.3 headless DMG and validated PKG code remain present.
- Confirmed no GitHub access or .NET compilation was used for this patch.
