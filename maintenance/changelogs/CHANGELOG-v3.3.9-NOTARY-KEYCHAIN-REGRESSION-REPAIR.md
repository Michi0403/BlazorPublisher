# PublisherStudio 3.3.9 — macOS notarization keychain regression repair

- Advanced PublisherStudio from 3.3.8 to 3.3.9 under the single-digit second/third-slot version policy.
- Applied the same macOS notarization regression repair as LocalGPT: the early `future2-notary` check is no longer the only keychain validation before a much later upload.
- Revalidates Apple trust immediately before every macOS RID and immediately before notarization.
- Fresh DMG/PKG artifacts use `notarytool submit --wait --timeout ...` so upload and Apple waiting remain inside one authenticated notarytool process.
- Retries once only when Apple explicitly reports `No Keychain password item found`, after an immediate profile revalidation.
- Existing SHA-256-bound notarization state remains resumable without re-uploading unchanged artifacts.
- Preserved PublisherStudio 3.3.8 `html-browser-chunked` PDF validation, embedded documentation, signing/stapling, packaging, localization/runtime behavior, and InteractiveServer boundaries.
