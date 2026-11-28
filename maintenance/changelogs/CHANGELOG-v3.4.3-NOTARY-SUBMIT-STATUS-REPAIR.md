# PublisherStudio 3.4.3 - Notary submit status repair

Mirrors the LocalGPT 3.8.1 post-upload notarization repair.

- A successful non-waiting `xcrun notarytool submit --output-format json` now requires only the submission `id`; PublisherStudio no longer assumes a `status` property is present in the submit response.
- Fresh submissions persist ID/SHA-256 and immediately enter the common `notarytool info` polling path.
- Resumed submissions use that same polling path without duplicated one-off status logic.
- Apple notary JSON and local notary-state JSON are read through StrictMode-safe optional-property access.
- A successful `notarytool info` response with valid JSON but no `status` is retried instead of causing a raw property error.
- Existing keychain/service retry, signing behavior, browser-PDF cache, shared LocalGPT.ReleasePackaging ownership, and InteractiveServer architecture are preserved.
