# PublisherStudio 4.6.5 — accurate LocalGPT link readiness and errors

- Reconnect no longer displays **“LocalGPT link restored”** merely because the TCP connection/HelloAck succeeded.
- The existing Editor `LocalGptConnection.Changed` handler now uses the common `Notifications` service: it reports a usable capability directory **only** after an accepted 1-Wire response and reports subsequent read-loop/cryptographic failures to the user (with duplicate notification suppression).
- The existing `UnprotectIncomingAsync` verifier and fail-closed peer trust remain unchanged. Signed/encrypted messages with missing, rotated or expired trust are **not** silently accepted.
- New `Assert-OneWireReadyNotifications.ps1` PowerShell build gate + Node contract test. Existing DI registrations, public APIs and version scheme preserved. Active project + installer versions are **4.6.5**.

## Limits
Receipt of an unencrypted, approved-loopback capability directory is **not** cryptographic proof. The UI therefore says "received", not "cryptographically verified". This change makes status truthful and errors visible; if the actual persisted reciprocal trust does not match, explicit MFA re-pairing is still required. It does not claim to restore AI features before that re-pairing.

Full Windows Razor/.NET build, PowerShell execution and physical PublisherStudio↔LocalGPT session have **not** been validated here.
