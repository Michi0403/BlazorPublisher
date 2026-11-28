# PublisherStudio 3.5.5 — installer, logging, and port recovery

PublisherStudio 3.5.5 repairs the Windows install/update/start path and restores durable diagnostics without undoing the 3.5.3 frontend fixes or the macOS cross-platform packaging/runtime work.

## Windows installation and update

- `%LOCALAPPDATA%\PublisherStudio` remains the canonical installation and user-data root. The former `BlazorPublisher` tree is not reused or migrated implicitly.
- Application and setup ZIPs are fully staged before the installed wrappers are touched.
- Staged `win*` and `setupwin*` payloads must carry matching `RELEASE-VERSION.txt` and `SOURCE-SHA256.txt` identities, and their managed assemblies must match the staged release version.
- The currently running installer version is deliberately **not** required to equal the incoming release version, so an older installed setup can update to a newer release.
- Only the packaged PublisherStudio runtime owned by the current `%LOCALAPPDATA%\PublisherStudio\win*` installation is stopped for replacement; alternate/debug hosts are preserved.
- `win*` and `setupwin*` are replaced as a rollback-capable transaction. User data beside those wrappers is never deleted by an ordinary install/update.
- The setup banner is derived from the installer assembly instead of the obsolete hard-coded `2.2.5` label.

## Durable diagnostics

- Setup writes `%LOCALAPPDATA%\PublisherStudio\PublisherStudio.Setup.log` from the beginning of a normal setup run and persists full exceptions.
- PublisherStudio writes `%LOCALAPPDATA%\PublisherStudio\PublisherStudio.log` by default instead of depending on the executable/current working directory.
- An early bootstrap/fatal path appends directly to the durable application log before/around the configured logging pipeline, and fatal exceptions are also written to stderr.
- macOS keeps its launcher diagnostic log while the application log now resolves directly beneath `~/Library/Application Support/PublisherStudio/PublisherStudio.log`.

## Windows loopback recovery

- Port `58071` remains the preferred/default PublisherStudio port.
- Before Kestrel is configured, PublisherStudio probes the requested loopback port. If Windows rejects it (including excluded/reserved port ranges such as `SocketException 10013`), PublisherStudio selects an OS-assigned loopback port instead.
- `server.json` remains authoritative for the actual runtime endpoint, so setup/browser discovery follows the effective port rather than assuming `58071` succeeded.

## Preserved behavior

- 3.5.3 RichEdit/frontend race mitigation, invariant export sliders, multi-picture Gallery ingestion, and cache-versioning remain intact.
- 3.5.3/3.5.4 macOS distribution-PKG validation/readback remains intact.
- `server.json` stays a cross-host rendezvous contract; installer ownership logic does not turn it into a single-host lock file.
- Render-mode ownership, PowerShell 5.1 documentation fixes, Debug/Release PDF contract, OrganicPlugins wording, and user-data layout remain unchanged except for the explicit logging durability repair above.
