# PublisherStudio 3.4.4 — artifact-local notarization transactions

- Mirrors LocalGPT 3.8.2 notarization semantics for every macOS DMG/PKG.
- `notarytool submit` is non-retrying and transaction-local; only idempotent history/info/log queries retry.
- Persists hash-bound pending state before upload and reconciles ambiguous submit results through Apple history instead of blind resubmission.
- Reruns resume unchanged pending/submitted artifacts and skip locally validated completed artifacts.
- No application/render-mode architecture changes.
