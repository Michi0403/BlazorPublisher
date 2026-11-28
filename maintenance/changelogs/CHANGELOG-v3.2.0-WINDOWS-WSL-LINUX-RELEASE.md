# PublisherStudio 3.2.0 — Windows/WSL Linux release

- Added optional Windows-coordinated WSL Linux x64/ARM64 release production with `Auto`, `Off`, and `Require` modes.
- Added explicit WSL setup/provisioning helpers; ordinary Windows developers without WSL retain the previous Windows-only behavior.
- WSL builds run from a Linux-filesystem source mirror and reuse parent-prepared DevExpress browser assets and documentation.
- DevExpress license material can be bridged from Windows without entering source or release artifacts; an optional user-only Linux profile copy is supported.
- The LocalGPT.ReleasePackaging package remains LocalGPT-owned/local-first. Windows can prepare only the package for WSL without installing the Unix tool locally.
- Linux Full/Light TAR.GZ and DEB remain mandatory. RPM/AppImage are optional; provisioned WSL can finish both without Docker.
- AppImage finishing now selects the target architecture through `ARCH` and enables extract-and-run under WSL.
- Native Linux/macOS developer/release paths and explicit InteractiveServer boundaries remain unchanged.
- WSL readiness explicitly requires WSL2, and the DevExpress license bridge uses the correct Windows-to-WSL environment direction/path translation.
- Delegated PublisherStudio children skip Node.js preflight because DevExpress browser assets and documentation are already parent-prepared.
