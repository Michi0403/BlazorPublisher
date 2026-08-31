# PublisherStudio 3.1.9 - macOS Linux cross-release and Homebrew RPM support

- Expands the normal macOS release lane to publish `osx-x64`, `osx-arm64`, `linux-x64`, and `linux-arm64` in one run. Windows remains Windows-only by default; Linux remains a supported native release host for other developers.
- Keeps Linux TAR.GZ and DEB creation cross-host through the LocalGPT-owned `LocalGPT.ReleasePackaging` helper, consumed local-first by PublisherStudio.
- Adds macOS RPM finishing through `rpmbuild`, including Homebrew `rpm` formula discovery. `brew install rpm` is the supported macOS prerequisite.
- Adds `-ProvisionNativePackagingTools` as an explicit opt-in to invoke `brew install rpm` when Homebrew is already present. PublisherStudio never installs Homebrew itself and ordinary release builds do not silently change the workstation.
- Uses explicit Linux RPM targets (`x86_64-unknown-linux` / `aarch64-unknown-linux`) to wrap the already-published Linux payload for the correct architecture on macOS or Linux.
- Keeps AppImage Linux-native. macOS skips it by default; `-UseContainerPackaging` remains an optional Docker/Podman path rather than a requirement.
- Adds `-RequireOptionalNativePackages` for release operators who want RPM/AppImage failures to be fatal. Without it, missing/failing optional finishers warn and the successful Windows/macOS/TAR.GZ/DEB outputs remain valid.
- Preserves local-first LocalGPT packaging consumption, one-click Windows setup behavior, the 3.1.8 macOS executable-mode repair, Panel Studio/editor behavior, application localization, and all explicit InteractiveServer boundaries.
