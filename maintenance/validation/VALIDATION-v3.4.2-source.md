# PublisherStudio 3.4.2 source validation

Source-only validation verifies version policy, PowerShell compatibility guards, a single startup trust preflight, absence of per-RID trust revalidation, absence of Read-Host in notarization scripts, direct xcrun notarytool retry orchestration, optional file-keychain forwarding, PDF chunk resume markers, shared packaging ownership, and InteractiveServer invariants. No macOS signing/notarization or dotnet build was executed in this environment.
