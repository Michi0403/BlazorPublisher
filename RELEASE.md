# PublisherStudio 3.4.5

PublisherStudio 3.4.5 mirrors LocalGPT 3.8.2's artifact-local Apple notarization state machine. Every macOS DMG and PKG owns an independent SHA-256-bound transaction. `notarytool submit` is non-idempotent and therefore never participates in the generic retry loop.

Before each new artifact upload, the release persists `submit-pending` state and captures a read-only Apple history baseline. The artifact is submitted once. If the local result is ambiguous, Apple history is reconciled and a matching new submission ID is adopted rather than blindly uploading again. `history`, `info`, and `log` remain retryable because they do not create new submissions.

See `CHANGELOG-v3.4.5-ARTIFACT-LOCAL-NOTARY-TRANSACTIONS.md` and `VALIDATION-v3.4.5-source.md`.

## Release behavior

- DMG and PKG notarization is independent and hash-bound per artifact.
- `submit` runs once per new transaction; history/info/log queries may retry.
- Pending/submitted/accepted/completed state survives reruns for unchanged bytes.
- Ambiguous submits are reconciled before any further upload can occur.
- Already stapled and locally validated artifacts are reused rather than resubmitted.
- Apple tooling is invoked through `xcrun notarytool`.
- Existing documentation, application architecture, and InteractiveServer behavior are unchanged.
