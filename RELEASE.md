# PublisherStudio 3.1.9

PublisherStudio 3.1.9 is the **macOS Linux cross-release and Homebrew RPM** maintenance release.

The default workstation release matrix now uses Windows for the Windows x64/x86/ARM64 application/setup outputs and macOS for macOS x64/ARM64 plus Linux x64/ARM64 application packages. Linux remains a first-class build host for developers and Linux-native release finishing.

On macOS, Linux TAR.GZ and DEB packages are created with the LocalGPT-owned managed packaging helper. RPM can be created with Homebrew's `rpm`/`rpmbuild` (`brew install rpm`) and explicit Linux targets. `-ProvisionNativePackagingTools` is an opt-in helper for installing that formula when Homebrew already exists. AppImage stays Linux-native and is skipped on macOS unless an optional container fallback is explicitly requested.

RPM/AppImage remain optional by default. `-RequireOptionalNativePackages` makes them strict when a release operator explicitly requires them.

See `CHANGELOG-v3.1.9-MACOS-LINUX-HOMEBREW-RELEASE.md` and `VALIDATION-v3.1.9-source.md`.
