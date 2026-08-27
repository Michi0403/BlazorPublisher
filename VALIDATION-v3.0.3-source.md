# PublisherStudio 3.0.3 source validation

This handoff is validated statically. No `dotnet` restore/build/test/publish and no PowerShell release build were executed in the packaging environment.

Validated source contracts:

- PublisherStudio Web and installer identities are `3.0.3`; browser cache identities and npm package identities match.
- The source preflight resolves both Python 3 and the pinned Node.js 22.23.2 runtime before long-running release work.
- DevExpress asset preparation imports `build/NodeRuntime.Common.ps1`, resolves/provisions Node.js cross-platform, and no longer requires a non-null Windows candidate-path collection.
- npm/npx discovery prefers tools adjacent to the resolved Node runtime and uses host-appropriate Windows/macOS/Linux fallbacks.
- The 3.0.2 host-neutral Python-backed source guards remain intact.
- Static release audit passed with 94 checks.
- Cross-platform boundary audit passed with 60 checks; no maintained System.Drawing/GDI or common-service OS leaks were detected.
- Application architecture audit passed.
- Async continuation audit passed for 80 source files.
- Panel Studio persistence audit passed.
- Iterator exception policy, service resilience (1375 service methods), component resilience (2687 component methods), and prerender JavaScript interop safety audits passed.
- XML documentation coverage/quality passed for 6270 direct C# declarations and 3443 direct Razor `@code` declarations.
- All 5 maintained render-mode directives are byte-for-byte equivalent at the directive-map level to 3.0.2 (SHA-256 `07e94a55768f6bd4ee027920d396bf83fc1702720038e61586efd81a462237b8`).
- Source ZIP validation requires repository-root layout, explicit directory entries, duplicate/case/Unicode collision checks and `unzip -t` integrity verification.

Run the authoritative project build/release scripts on licensed Windows/macOS/Linux build hosts before publication.
