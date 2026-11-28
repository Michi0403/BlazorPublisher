# PublisherStudio 3.4.2 - Notary orchestration recovery

Mirrors the LocalGPT 3.8.0 notarization orchestration repair.

- Removes redundant per-RID trust revalidation.
- Removes blocking `Read-Host` recovery from notarization.
- Removes redundant pre-submit `notarytool history` probes.
- Retries intermittent keychain/profile and Apple/network failures at the actual `xcrun notarytool` operation.
- Corrects guidance to `xcrun notarytool` and supports optional `MACOS_NOTARY_KEYCHAIN_PATH`.
- Preserves PowerShell compatibility, chunked browser PDF rendering/cache, shared packaging ownership, signing behavior, and InteractiveServer architecture.
