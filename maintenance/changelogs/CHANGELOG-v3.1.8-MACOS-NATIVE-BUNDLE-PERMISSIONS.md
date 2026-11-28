# PublisherStudio 3.1.8 - macOS native bundle permission repair

- Preserves the 3.1.7 host-aware release matrix: normal macOS release builds select only `osx-x64` and `osx-arm64`; Linux RPM/AppImage finishing is never required on macOS.
- Repairs executable file modes inside generated PublisherStudio macOS `.app`/DMG payloads. The generated bundle launcher, published `PublisherStudio.Web` apphost, and `install-dependencies.sh` are explicitly marked executable before packaging.
- Emits `Info.plist` through the shared UTF-8-no-BOM writer to avoid host-dependent encoding differences.
- Hardens AppImage staging on Linux by explicitly marking `AppRun` and the application apphost executable. RPM/AppImage remain optional Linux-native finishers; missing tools warn and skip.
- Docker/Podman remains opt-in only. LocalGPT.ReleasePackaging remains LocalGPT-owned and at 1.0.1; PublisherStudio continues local-first consumption.
- No editor, recording/export, Panel Studio, application localization, or InteractiveServer runtime behavior changed.
