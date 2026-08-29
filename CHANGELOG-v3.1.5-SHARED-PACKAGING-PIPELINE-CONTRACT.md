# PublisherStudio 3.1.5 — Shared Packaging Pipeline Contract

## Fixed

- Hardened PublisherStudio's consumption of the LocalGPT-owned `LocalGPT.ReleasePackaging` tool so `dotnet tool install` progress text cannot contaminate the captured executable-path result.
- `Build-Release.ps1` now requires the shared packaging helper to return exactly one executable path and verifies that path before native packaging starts.
- This mirrors the LocalGPT 3.5.2 fix for the PowerShell success-pipeline behavior that caused the reported Linux packaging parameter-binding failure.

## Shared installer/release architecture

- PublisherStudio continues to consume the LocalGPT-owned release-packaging tool in the same shared-package style as the 1-Wire protocol package rather than carrying a duplicate packaging project.
- Windows keeps the PublisherStudio installer console and one-click setup workflow.
- Linux uses Full/Light payloads plus `.tar.gz`, `.deb`, `.rpm`, and `.AppImage` packages rather than a console setup executable.
- macOS uses Full/Light `.app`/`.tar.gz` packaging with native `.dmg` finishing on macOS.
- Existing Windows application behavior, recording/export paths, `InteractiveServer` boundaries, and typed runtime-policy ownership are unchanged.
