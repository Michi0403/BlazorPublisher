# PublisherStudio 3.3.0 — automatic macOS public-release trust and signing

## Fixed

- A normal `Build-Release.ps1` run on macOS now requires distributable trust for selected `osx-*` outputs by default. The operator no longer has to remember to export `MACOS_REQUIRE_NOTARIZATION=1` before a public build.
- Added an early macOS trust preflight before DevExpress asset preparation, documentation rendering, and RID publishing. It discovers the installed `Developer ID Application` and `Developer ID Installer` identities and validates notarization credentials before the long release pipeline starts.
- The standard Future2 build-machine profile `future2-notary` is selected automatically when no explicit notarization credential configuration is present. Existing keychain-profile, App Store Connect API-key, and Apple-ID/team/app-specific-password overrides remain supported.
- Added `-AllowUnsignedMacPackages` for intentional local-only builds. This keeps public distribution safe by default without removing the contributor/developer path.
- The application bundle now explicitly signs and verifies every Mach-O payload and signs nested framework/plugin/XPC/app code containers deepest-first before signing the enclosing `.app`.
- DMG output is now Developer ID Application-signed before submission to Apple's notary service, then receives staple validation, signature validation, and a Gatekeeper disk-image assessment.
- PKG output remains Developer ID Installer-signed and notarized/stapled. It additionally performs `pkgutil --check-signature` plus a visible local Gatekeeper installer assessment. The `spctl` result remains diagnostic rather than replacing Apple's Accepted notarization result and staple/signature checks.
- macOS builds that cross-publish Windows payloads now state clearly that Apple Developer ID cannot establish Windows Authenticode trust. A separate Windows code-signing identity is required for that platform.

## Operator behavior

After creating the two Developer ID certificates and storing notarization credentials once as `future2-notary`, the normal signed/notarized PublisherStudio release command is simply:

```powershell
pwsh Build-Release.ps1
```

No signing secrets are embedded in the repository or final packages.

## Preserved

- PublisherStudio application behavior, four reviewed `InteractiveServer` directives, the 3.2.7 method-diagnostics repair, 3.2.8 DocFX PDF fallback repair, architecture/Rosetta checks, Future2/licensing documentation, DevExpress preparation, staging cleanup, and Windows/Linux packaging behavior remain unchanged.

## Version

- Version rolled from 3.2.9 to 3.3.0 because the project uses single-digit minor and patch slots and the release PowerShell behavior changed.
