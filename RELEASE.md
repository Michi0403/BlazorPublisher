# PublisherStudio 3.2.4

PublisherStudio 3.2.4 applies the same installed macOS launcher repair as LocalGPT: the `.app` launcher now checks the runtime endpoint file and the authoritative `http://127.0.0.1:58071` endpoint, allows slow startup for up to five minutes, opens the browser when HTTP is actually ready, and opens a Terminal log-follow helper after 20 seconds rather than declaring failure after 30 seconds.

The release pipeline also addresses the 3.2.3 `No space left on device` failure. After each Unix Full/Light native package set is successfully created and validated, its documentation-bearing staging tree and transient macOS `.app` working bundle are removed immediately. The durable DocFX cache and final release artifacts are retained; only redundant temporary working copies are released.

The headless DMG and explicit `/Applications/PublisherStudio.app` PKG layout introduced in 3.2.3 remain unchanged.

See `CHANGELOG-v3.2.4-MACOS-LAUNCHER-WORKSPACE-REPAIR.md` and `VALIDATION-v3.2.4-source.md`.
