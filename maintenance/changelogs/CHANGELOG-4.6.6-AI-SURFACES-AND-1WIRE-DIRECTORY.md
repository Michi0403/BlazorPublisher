# PublisherStudio 4.6.6 — Restore AI surfaces and 1-Wire directory lifecycle

Base: full PublisherStudio 4.6.5 source archive supplied by the user. No GitHub fetch, dotnet restore/build/test, or deployment was performed.

## Repairs

- Keep the `LocalGPT AI` ribbon, picture/video/audio/panel AI entries, AI Text quick insert, Story Editor AI ribbon/panel, Spreadsheet AI workspace, the OpenSCAD/Spreadsheet team entries, and selected-object context-menu actions **visible during connection and capability renegotiation**. Opening an editor or reviewing existing proposals is distinct from invoking remote AI.
- Keep actual `council.run` permission checks in the owning command paths and AI bridge. An unavailable capability is explained within Story/Spreadsheet/Media authoring surfaces instead of hiding the surface; it is not treated as permission to execute.
- Accept a cryptographically validated capability/skill directory into the **current authenticated TCP connection state** even if the UDP discovery peer has expired or was deliberately suspended. Apply its metadata to discovery only when the entry exists. Ignore responses from stale connection IDs and prevent overwriting a discovered usable address with `0.0.0.0`.
- On opening the 1-Wire security panel with an existing identity, renew the signed, short-lived **public pairing ticket** for display. A separate `Renew public pairing ticket` button is available without reading the MFA seed. The existing explicit Authenticator enrollment action remains separate; no private key, MFA seed, or stored trust entry is regenerated.

## Guarded behavior retained

Mutual 1-Wire trust, encryption/signature validation, authorization, and explicit mutation approval remain enforced; no capability is automatically granted to an unlinked or untrusted peer. Native publisher content operations remain user initiated.

## Offline verification

- `node --test tests/js/*.test.cjs tests/js/*.test.mjs`: **32 passed**, including four new source-contract assertions for AI visibility, guarded send, discovery-independent current-session capability updates, and public ticket display.
- Project XML parsed; complete source ZIP integrity verified after packaging.
- **Not verified**: .NET/Razor compilation, Windows/DevExpress UI rendering, reciprocal 1-Wire runtime test, or end-to-end provider execution.
