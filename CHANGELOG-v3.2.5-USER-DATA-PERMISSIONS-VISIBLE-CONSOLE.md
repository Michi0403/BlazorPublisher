# PublisherStudio 3.2.5 — user-data permissions and visible console

- Makes the installed macOS launcher open its Terminal log console immediately by default, matching the diagnosable console-oriented startup used on Windows/Linux. Set `PUBLISHERSTUDIO_SHOW_CONSOLE=0` for an intentionally quiet macOS launch.
- Creates and write-probes the per-user `~/Library/Application Support/PublisherStudio`, runtime, Logs, and Caches directories before startup.
- If one of those PublisherStudio-owned user directories has incorrect ownership, the launcher can request a scoped administrator repair for that directory only; `/Applications/PublisherStudio.app` remains read-only application content.
- Creates the launcher log before Terminal follows it, avoiding a first-run log-tail race.
- Changes new 1-Wire secret storage to prefer per-user application data while preserving an existing writable portable secret.
- Hardens `ApplicationPathService` so missing OS known-folder values fall back to per-user PublisherStudio application data rather than a relative/application-bundle path.
- Linux AppImage desktop launches now request a visible terminal and verify per-user XDG data/state/cache directories before starting PublisherStudio.
- Preserves the five-minute HTTP readiness launcher behavior, disk-workspace cleanup, headless DMG, and validated PKG layout from 3.2.4/3.2.3.
- Version advanced from 3.2.4 to 3.2.5.
