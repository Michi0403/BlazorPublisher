# PublisherStudio 3.2.4 — macOS launcher and workspace repair

- Applied the installed macOS launcher readiness repair with the authoritative PublisherStudio endpoint `http://127.0.0.1:58071`.
- Replaced the fixed 30-second endpoint-file-only startup decision with endpoint-file plus HTTP readiness probing for up to five minutes.
- Added a Terminal startup-log follower after 20 seconds so a slow host remains diagnosable while the application continues starting.
- Opens the browser as soon as the real local HTTP endpoint responds.
- Releases each completed Unix RID staging tree and transient `PublisherStudio.app` working bundle after native package validation, addressing the 3.2.3 disk-exhaustion failure caused by repeated documentation-bearing working copies.
- Preserves the durable DocFX cache and final TAR/DMG/PKG artifacts.
- Version advanced from 3.2.3 to 3.2.4.
