# PublisherStudio 3.2.2 — macOS coordinator, Pages and packaging repair

- Fixed two additional launcher defects found during final validation: endpoint discovery no longer runs inside a pipeline subshell (which discarded successful returns on macOS `/bin/sh`), and the launcher no longer passes the unsupported bare `--no-browser` argument into ASP.NET configuration.
- Fixed the shared packaged macOS launcher to read `BaseUrl`, reject stale endpoint metadata, log startup, and report failures natively instead of opening bare `127.0.0.1`.
- Added native `.icns` generation, application metadata, ad-hoc signing, branded writable-DMG Finder layout, and native PKG output when `pkgbuild` is available.
- macOS `-Runtime all` now coordinates macOS x64/ARM64, Linux x64/ARM64, and Windows x64/x86/ARM64 application/setup builds.
- Fixed the 3.2.1 Pages failure by making the tracked Pages ZIP HTML-only. The mandatory release PDF remains in release documentation and is represented in Pages status metadata; PDF links are redirected to the latest release.
- Avoided reading huge HTML-accessibility-fallback PDFs fully into Python memory merely to determine tagging; tagged-PDF-required mode still verifies the complete PDF bytes.
- Kept LocalGPT-owned local-first packaging, optional WSL2 Linux delegation, native Linux support, Homebrew-aware RPM, and Linux/WSL AppImage finishing.
