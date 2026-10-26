# PublisherStudio 4.6.4 — 1-Wire identity persistence (source candidate)

## Repaired

- Stable per-user 1-Wire identity takes precedence if both persistent and portable identity files exist. No automatic identity rotation or trust bypass.
- If only the portable identity exists, copy it to a new durable user-data location with restricted permissions and atomic rename, preserving the original, signing keys, MFA seed and peer trust. The portable original is not deleted.
- In the event the persistent identity is inaccessible, do not create a replacement automatically. Retain fail-closed behavior.
- Mandatory `Assert-OneWireIdentityPersistence.ps1` build gate and Node source tests.

## Still open / not asserted

- Existing mismatched peer fingerprints require verified **reciprocal** re-pairing. Merely connecting the transport is not authenticated trust. No security bypass is introduced.
- The optional TLS server certificate wizard/endpoint, GPU media generation through LocalGPT and complete PublisherStudio browser automation have not been implemented by this patch.
- No live full-stack Windows/.NET/PowerShell validation was available; successful connection and persistent MFA after restart have not been confirmed.
