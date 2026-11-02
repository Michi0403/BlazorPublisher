# PublisherStudio 4.7.1 — restore manual 1-Wire connection without UDP dependency

- Restore an always-visible manual Connect action on LocalGPT AI Assist, even if UDP 51141 discovery has found no peer.
- Allow the user to enter one or more LocalGPT application origins using `http://` and/or `https://`, one per line. Resolve the authenticated peer identifier and advertised 1-Wire TCP service port through LocalGPT's maintained public `/api/onewire/http-json/profile` route. Explicit origins are tried in order and failures are shown without silently changing scheme.
- Verify response identity (`Application == LocalGPT`), reject credentials/non-origin URL inputs and unexpected redirects to other origins. HTTPS uses normal platform certificate validation—no bypass.
- Once resolved, connect through the existing TCP transport, MFA trust, approval, Hello/HelloAck and capability negotiation. Existing UDP-discovered Connect actions, auto-connect-to-discovered-local-peers policy, transport security and work handling are unchanged.
- Add browser-persisted manual origins and opt-in reconnect when this frontend page is opened; this is not a service-wide background connection and does not circumvent permission/approval.
- Distinguish saved reciprocal 1-Wire trust records from a current live link so a disconnected TCP session is not represented as a revoked trust relationship.
- Preserve existing Story Editor/Spreadsheet/Picture Studio and organic application command functionality.

Scope note: HTTP and HTTPS provide web-origin discovery for connection bootstrap; Council work still uses the existing protected 1-Wire TCP session. This does not introduce a separate, HTTP-only work transport. Neither certificate trust nor MFA is bypassed.

Verification status: Source inspection only. This environment lacks `dotnet`, `pwsh`, and `powershell`; runtime, build, Windows/Linux integration and full round-trip verification must still be run on the target machine. No Python or Node tests were added.
