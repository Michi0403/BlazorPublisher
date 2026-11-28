# PublisherStudio 3.2.9 source validation

Static/source validation only; no .NET restore/build/publish, DocFX render, GitHub access, Apple notarization submission, or macOS package production was performed in this environment.

- Confirmed application, installer, npm, documentation metadata/PDF name, and browser cache-buster version metadata is 3.2.9.
- Confirmed macOS packaging resolves Developer ID Application and Developer ID Installer identities from explicit environment variables or installed keychain identities.
- Confirmed Developer ID application signing enables hardened runtime and timestamping and verifies the resulting `.app`.
- Confirmed nested Mach-O payloads are signed before the enclosing bundle and the .NET apphost receives `com.apple.security.cs.allow-jit` for Hardened Runtime JIT compatibility.
- Confirmed PKG generation uses Developer ID Installer signing when available while retaining explicit `/Applications/PublisherStudio.app` payload validation.
- Confirmed DMG and PKG artifacts support `notarytool submit --wait`, `stapler staple`, and `stapler validate`.
- Confirmed notarization credentials can be supplied through a notarytool keychain profile, App Store Connect API key variables, or Apple ID/team/app-specific-password variables.
- Confirmed `MACOS_REQUIRE_NOTARIZATION=1` is available as a public-release gate and missing credentials otherwise produce a clear Gatekeeper warning instead of being mistaken for a trusted release.
- Confirmed the 3.2.7 method-diagnostics fix and 3.2.8 DocFX PDF fallback repair remain present.
- Confirmed the four maintained `@rendermode InteractiveServer` declarations remain unchanged in count.
- Confirmed no repository-local `bin` or `obj` directories are included in the delivered source ZIP.

- Architecture policy audit passed for the maintained application boundaries.
- Async-continuation policy audit passed for the maintained source tree.
- Service-resilience audit passed for maintained service methods.
