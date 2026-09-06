# PublisherStudio 3.3.0 source validation

Validation is source-only in this environment. No `dotnet`, MSBuild, DevExpress restore/build, GitHub access, Apple signing, Apple notarization, or application launch was performed. PowerShell is unavailable in this validation container, so release-script checking is static plus delimiter/string-aware lexical validation rather than execution.

Checked statically:

- PublisherStudio application/setup and browser-package versions roll correctly from 3.2.9 to 3.3.0 under the single-digit minor/patch policy;
- `Build-Release.ps1` automatically invokes the macOS trust preflight for selected `osx-*` runtimes and provides `-AllowUnsignedMacPackages` only as an explicit local-development opt-out;
- the preflight discovers Developer ID Application and Installer identities, defaults to the shared `future2-notary` keychain profile when appropriate, validates that profile before the long asset/documentation pipeline, and makes notarization mandatory for the normal macOS release path;
- source-package preflight now requires the macOS trust helper so it cannot disappear from a source archive unnoticed;
- every discovered Mach-O payload is signed and verified, nested code containers are signed deepest-first, and the enclosing app retains hardened-runtime/timestamp signing;
- DMG output is Developer ID Application-signed before notarization and receives staple/signature/Gatekeeper-open validation;
- PKG output remains Developer ID Installer-signed, notarized, stapled and `pkgutil`-validated, with a visible local Gatekeeper installer assessment;
- Apple Developer ID is explicitly not presented as Windows signing; Windows still requires a separate Authenticode certificate;
- the 3.2.7 `ApplicationPathService` diagnostics repair, 3.2.8 DocFX fallback repair, architecture/Rosetta checks and all four reviewed `@rendermode InteractiveServer` directives remain intact;
- no repository-local `src/**/bin` or `src/**/obj` build state is included.

The authoritative end-to-end check remains a real macOS `pwsh Build-Release.ps1` run on the configured Apple Developer machine, followed by testing a freshly downloaded/quarantined release artifact on a separate Mac.
