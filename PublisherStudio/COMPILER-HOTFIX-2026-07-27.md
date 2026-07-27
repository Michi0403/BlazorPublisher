# BlazorPublisher / PublisherStudio 2.0.1 — Organic 1-Wire 2.1 compiler hotfix

This source package continues the Organic 1-Wire 2.1 runtime security, MFA, OCR, HTTP/JSON, TextStudio and responsive UI work and fixes the compiler failures reported on 2026-07-27.

## Fixed

- `OrganicRuntimeSecurityService` now consistently implements the interface with `OrganicWireEnvelope` and `OrganicWireMessageType` aliases from the authoritative `LocalGPT.WireProtocolVersion` NuGet package.
- `PictureEditor.razor.cs` imports `PublisherStudio.Services.UserExperience`, resolving `IUserNotificationService` in code-behind compilation.
- Razor imports now include `PublisherStudio.Components.OrganicPlugins`, resolving `OrganicSecurityPanel` as a component instead of unexpected markup.
- `Build-Release.cmd` and `Build-AllRuntimes.cmd` now set the repository working directory, preserve the exit code, and pause on failure.
- Added regression checks for the exact compiler and Razor errors.

## First build

1. Close Visual Studio.
2. Build LocalGPT first so `LocalGPT.WireProtocolVersion.2.1.0.nupkg` is available in the configured package cache/release source.
3. Replace the old PublisherStudio source directory cleanly; do not overlay it.
4. Delete `.vs`, `bin`, and `obj`.
5. Run `Build-LocalDevelopment.cmd`.
6. Prepare licensed DevExpress browser assets when required, then run `Build-Release.cmd` or `Build-AllRuntimes.cmd`.

PublisherStudio contains no protocol source project and no committed `.nupkg`; it consumes the authoritative LocalGPT protocol package only.
