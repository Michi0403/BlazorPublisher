# PublisherStudio 3.1.7 - host-aware local-first release

- Changed default `Build-Release` runtime selection to follow the current host:
  - Windows builds `win-x64`, `win-x86`, and `win-arm64` application/setup outputs only.
  - Linux builds `linux-x64` and `linux-arm64` application packages.
  - macOS builds `osx-x64` and `osx-arm64` application packages.
- Added explicit `-Runtime all-rids` for deliberate cross-host publish attempts without making them the normal release path.
- Windows-only releases no longer prepare or install `LocalGPT.ReleasePackaging`, because no Unix packaging step needs it.
- When Unix/macOS packaging is selected, the shared `LocalGPT.ReleasePackaging` package is resolved local-first: PublisherStudio package cache, explicit/ambient LocalGPT checkout, shared LocalGPT NuGet cache, and the standard installed LocalGPT source location. If a LocalGPT checkout is available but the package has not been built yet, PublisherStudio invokes LocalGPT's authoritative package publisher instead of duplicating the packaging source.
- Network download of `LocalGPT.ReleasePackaging` is no longer implicit. It occurs only when `-ReleasePackagingPackageUrl` is explicitly supplied.
- RPM/AppImage are optional Linux-native finishers. Missing native tools no longer fail a release, and Docker/Podman is used only when `-UseContainerPackaging` is explicitly requested.
- Added UTF-8 console initialization to Windows release/development entry points to prevent `dotnet` UTF-8 output from being decoded as the legacy Windows console code page.
- No editor, recording/export, Panel Studio, localization, or InteractiveServer runtime behavior was changed.
