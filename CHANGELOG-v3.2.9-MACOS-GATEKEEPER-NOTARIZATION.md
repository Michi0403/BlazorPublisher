# PublisherStudio 3.2.9 — macOS Gatekeeper signing and notarization

## Fixed

- Replaced the release-only ad-hoc macOS signing path with Developer ID aware signing. The `.app` uses an installed or explicitly supplied `Developer ID Application` identity, hardened runtime, timestamping, and signature verification.
- The actual .NET apphost and every nested Mach-O payload are signed before the enclosing bundle. The non-NativeAOT apphost receives the `com.apple.security.cs.allow-jit` entitlement required by current .NET macOS guidance under Hardened Runtime.
- PKG creation now uses an available `Developer ID Installer` identity and validates the package signature.
- DMG and PKG outputs can now be submitted to Apple's notary service with `xcrun notarytool`, then stapled and validated before they are returned as release artifacts.
- Supported credential paths are `MACOS_NOTARY_KEYCHAIN_PROFILE`, App Store Connect API-key variables (`APPLE_NOTARY_KEY_ID`, `APPLE_NOTARY_ISSUER`, `APPLE_NOTARY_KEY_PATH`), or Apple ID/team/app-specific-password variables (`APPLE_NOTARY_APPLE_ID`, `APPLE_NOTARY_TEAM_ID`, `APPLE_NOTARY_PASSWORD`).
- Added `MACOS_REQUIRE_NOTARIZATION=1` for public-release builds. It fails macOS packaging if distributable signing/notarization cannot be completed.
- Local builds without Apple credentials remain possible but now state plainly that an ad-hoc/unnotarized PKG or DMG can be blocked once another Mac receives it as a quarantined Internet download.

## Why

PublisherStudio packages opened on the build Mac are not a reliable Gatekeeper test. A browser/WhatsApp/Mail download on another Mac can be marked with Apple's quarantine attribute. Ad-hoc signatures do not establish Developer ID trust, so Gatekeeper can report that Apple cannot verify the package for malicious software even though the same locally produced file opened on the build machine.

## Preserved

- PublisherStudio application behavior, 3.2.7 method-diagnostics repair, 3.2.8 documentation PDF fallback repair, Future2/licensing text, architecture/Rosetta checks, headless packaging, staging cleanup, and the reviewed four `InteractiveServer` boundaries are unchanged.

## Version

- Version advanced from 3.2.8 to 3.2.9 because the shared macOS release-packaging behavior changed.
