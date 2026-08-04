# PublisherStudio 2.0.2 — installer, release and runtime repair

PublisherStudio 2.0.2 is a preservation-first repair release. It restores the installer and launcher contract, keeps PublisherStudio usable without LocalGPT, and repairs the long-running recording-preview failure without removing existing publishing features.

## Closed

- **Existing launcher compatibility:** release archives again contain the historical runtime wrappers such as `winx64/` and `setupwinx64/`. Windows application ZIPs begin with a hash-catalogued setup-repair prelude, so an older setup stages the repaired launcher and standalone repair executable before it can encounter a locked running application file.
- **Running setup replacement:** Windows setup archives carry `PublisherStudio.Setup.repair.exe`, and every launcher promotes that staged standalone binary before invoking setup. The locked `PublisherStudio.Setup.exe` is deliberately written last in the setup archive; the application archive also carries the repair prelude first, allowing a broken existing launcher to heal even when application files are locked.
- **Preservation-first application updates:** the repaired installer validates both archives before stopping PublisherStudio, stages extraction, verifies the schema-2 file catalogue and SHA-256 hashes, and merges managed files into the runtime directory with per-file backup and rollback. The runtime directory is never deleted or moved during update.
- **Managed stale-file cleanup:** only files owned by the previous schema-2 release manifest may be removed, and only while their current SHA-256 still matches the previous release catalogue. Unknown files, user content and locally modified former release files remain untouched.
- **Setup repair:** setup and launcher files are replaced through the stable `setup<runtime>` path. When setup updates itself, a delayed file-level merge runs after the setup process exits instead of renaming the directory used by the launcher. Exact setup-directory detection is supported, so running directly from `setupwinx64` still schedules the safe replacement.
- **Release integrity:** application and setup versions must match; publish output must contain required configuration and launcher files; release manifests catalogue every payload file; uncatalogued, duplicate, traversal, oversized and symbolic-link ZIP entries are rejected.
- **Repository/path consistency:** publish profiles and `Build-Release.ps1` now agree on all seven supported runtime output folders and use the authoritative LocalGPT wire-protocol package version 2.1.1. Existing installations retain their installed runtime architecture and are never redirected to a payload from another operating-system family.
- **Optional 1-Wire boot:** UDP discovery remains available on 51141, but automatic transport connection is disabled by default. PublisherStudio starts and works normally when LocalGPT is absent or the discovery port is unavailable.
- **Side-by-side defaults:** PublisherStudio remains on loopback web port 58071, separate from LocalGPT's web surface and from the 1-Wire TCP/UDP contracts.
- **Recording preview resilience:** active camera/screen recording keeps its live `MediaStream` attached when Blazor replaces the preview `<video>` element. Watchdog cleanup is bounded and benign browser play interruptions are ignored; the saved recording path is unchanged.
- **Destructive-cleanup removal:** the updater no longer recursively deletes arbitrary `MediaHost` or `PublisherStudio.MediaHost*` paths. Release cleanup is limited to unchanged files proven to be owned by the previous schema-2 manifest; explicit uninstall remains the only whole-install removal path.
- **Runtime ownership boundary:** a runtime endpoint is removed only when it is stale or the installer successfully stops the PublisherStudio process it owns. A process outside the selected installation root is refused without erasing its endpoint evidence.

## Partial

- Native .NET 10, Windows launcher, delayed self-update, DevExpress and real camera/screen acceptance require maintainer execution on the licensed Windows build machine.
- Historical installations that execute an older setup binary may need to run the updated launcher a second time after the first release extraction stages `PublisherStudio.Setup.repair.exe`.

## Deferred

- DocFX generation and repository-wide XML documentation expansion are intentionally deferred to a later quality pass.
- Broader optional 1-Wire workflow redesign remains separate from this installer/runtime recovery release.
