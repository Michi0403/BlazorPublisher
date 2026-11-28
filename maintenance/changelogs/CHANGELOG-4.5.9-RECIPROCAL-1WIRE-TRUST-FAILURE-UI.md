# PublisherStudio 4.5.9 — Reciprocal 1-Wire trust failure visibility

## Verified evidence

The supplied PublisherStudio `OrganicPlugins/Security/onewire-secret.json` contains zero trusted peers while the LocalGPT secret records a trusted PublisherStudio identity. Trusting only one side does not establish mutual encrypted 1-Wire communication.

## Change

- Keep cryptographic peer verification fail-closed.
- Explicitly display the missing reciprocal trust record in PublisherStudio's security panel.
- Show the connection failure with a screen-reader-readable alert and concrete instructions to exchange the current LocalGPT pairing ticket, verify the local PublisherStudio MFA code and trust the peer in this direction.
- Add mandatory build/release PowerShell guard protecting verification and frontend failure visibility.

No automatic trust grant, key rotation, TLS fallback, or token reset was introduced. The live signed/secure re-pair flow still requires Windows acceptance testing.
