# PublisherStudio 3.1.7

PublisherStudio 3.1.7 is the **host-aware local-first release** maintenance release.

The supplied Windows 3.1.6 release log proves PublisherStudio successfully builds its application, documentation, all Windows x64/x86/ARM64 application/setup outputs, and Linux TAR.GZ/DEB artifacts before the old pipeline fails at mandatory RPM packaging. Windows release builds now stop at Windows outputs by default and therefore do not require the LocalGPT Unix packaging tool.

`Build-Release -Runtime all` now means all maintained runtimes for the current host OS. `-Runtime all-rids` is available for deliberate cross-host publishing. Linux RPM/AppImage finishing is optional, and container fallback is opt-in.

For non-Windows packaging, `LocalGPT.ReleasePackaging` remains LocalGPT-owned and is resolved local-first. PublisherStudio can consume the local package/cache or invoke the authoritative LocalGPT package publisher from an available LocalGPT checkout. Network download is used only when an explicit package URL is supplied.

See `CHANGELOG-v3.1.7-HOST-AWARE-LOCAL-FIRST-RELEASE.md` and `VALIDATION-v3.1.7-source.md`.
