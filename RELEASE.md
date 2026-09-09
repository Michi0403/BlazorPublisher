# PublisherStudio 3.5.2

PublisherStudio 3.5.2 fixes the macOS `Build-Release.ps1` parser error in the 3.5.1 published-assembly identity diagnostic by delimiting the release-mode variable before the literal colon (`${mode}:`). The source release audit now guards the same interpolation mistake.

The release also preserves the established `server.json` rendezvous behavior: updater ownership checks remain strict for the installed `/Applications/PublisherStudio.app`, while legitimate alternate/debug hosts are not terminated or erased merely because their executable/version differs from the packaged app. Installer endpoint cleanup is now ownership-aware.

The 3.5.1 macOS runtime/source identity and updater handoff remain intact, as do the 3.5.0 PowerShell 5.1 documentation-cache and Debug-PDF fixes.

See `CHANGELOG-v3.5.2-RELEASE-PARSER-RENDEZVOUS-REPAIR.md` and `VALIDATION-v3.5.2-source.md`.
