# PublisherStudio 4.7.3 — 1-Wire traffic journal

## Added

- **Every 1-Wire packet is journaled.** A new `IOrganicWireTrafficJournal` (singleton) records each packet PublisherStudio sends or receives:
  - TCP envelopes to and from LocalGPT, in both directions. Rejected incoming frames (bad JSON, failed decryption, peer-identity mismatch, missing protection, replay) are journaled with the reason before the read loop stops, and failed sends are journaled too.
  - The HTTP/JSON profile request and response used by **Connect by URL**.
  - Requests and responses of the HTTP/JSON add-on endpoint (`api/organic/onewire/http-json`), including rejected requests and its profile endpoint.
  - Every UDP discovery datagram received while discovery listens, including oversized, malformed and non-LocalGPT datagrams with the reason they were ignored.
- **Configurable logging** under the category `PublisherStudio.Services.OrganicPlugins.OrganicWireTrafficJournal`:
  - Information: one metadata line per packet. Rejected or failed packets are logged at Warning.
  - Debug: the decrypted payload, according to `PayloadMode`.
  - Trace: the raw frame exactly as it crossed the wire.
- **Settings** in `OrganicPlugins:Traffic`: `Enabled`, `PayloadMode` (`Full` by default, `Redacted`, `None`), `IncludeRawFrame`, `IncludeDiscovery`, `JournalCapacity`, `MaximumStoredCharacters`, `MaximumLoggedCharacters` and `RedactedPropertyNames`. They are bound live through `IOptionsMonitor<OrganicWireTrafficOptions>`, so edits to `appsettings.json` or the user settings file apply without a restart.
- **In-app view.** A new `OrganicWireTrafficPanel` sits in **Protocol settings and diagnostics** on the LocalGPT AI Assist page. It offers:
  - text search;
  - direction and transport filters;
  - a problems-only filter;
  - pause and clear;
  - a packet grid with payload and raw frame for the selected packet.

  Refreshes are coalesced to at most four per second. Clearing the view never touches log files.
- Localized UI text in all six catalogs.
- Documentation section "Traffic journal" in `docs/articles/localgpt-and-onewire.md`.

## Changed

- `LocalGptConnectionService.ConnectByWebUrlAsync` reads the profile body once as text, journals it, and then parses that same text. Origin checks, redirect rejection and the authenticated transport are unchanged.
- `OrganicWireHttpController` protects, serializes and journals responses through one private `RespondAsync`. Protection rules and status codes are unchanged.
- Synchronized the Web and Setup project versions from 4.7.2 to 4.7.3.

## Security note

`Full` payload mode at Debug writes decrypted content (prompts, documents, capability arguments) to the configured log sinks. This is intended for debugging the protocol between your own applications. Use `Redacted` or `None` when logs leave the machine. Raw frames at Trace stay encrypted for MFA-protected peers.

## Unchanged

- 1-Wire protocol, envelope format, MFA trust, replay protection, approval flows and discovery policy. The journal only observes; journaling failures are logged and never interrupt a transport.
- LocalGPT 7.0.1 ships the matching journal on its side (`OneWire:Traffic`).

## Verification status

Offline source contracts only:

- `tests/js/onewire-traffic-journal-473.test.cjs` and every existing `tests/js/*.test.cjs` pass under Node.
- Python emulations of the service-resilience, logging-integrity, text-service-ownership, system-variable and localization guards report no findings.

There is **no claim of a successful PowerShell guard run, .NET compile, Razor generation or live 1-Wire session**; `pwsh` and `dotnet` are unavailable in this environment.

## Interaction / frontend release checklist

- [x] New UI is one DevExpress-owned component (`DxFormLayout` boundary, `DxTextBox`, `DxComboBox`, `DxCheckBox`, `DxButton`, `DxGrid`, `DxMemo`). It has no native interactive controls and no new `<details>` disclosure, because it lives inside the existing diagnostics disclosure.
- [x] No canonical object layer, Mainframe Z-order, editor mode, overlay, pointer capture or publication render path is touched.
- [x] The component owns exactly one typed `ILogger<OrganicWireTrafficPanel>`. Refresh failures are logged and shown once through `IUserNotificationService`. A circuit disconnect during a pending refresh is a Debug-level diagnostic.
- [x] The journal subscription and the pending refresh are released on disposal. Background refreshes run through `ISupervisedTaskRunner`.
- [x] Mouse, keyboard and touch use the stock DevExpress grid and editor behavior; no custom input routing was added.
- [x] Regression test: `tests/js/onewire-traffic-journal-473.test.cjs`.
