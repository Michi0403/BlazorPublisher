# LocalGPT and 1-Wire

PublisherStudio works without LocalGPT. The 1-Wire connection is an optional local collaboration path.

## Discovery and connection

PublisherStudio keeps LocalGPT discovery network activity idle by default. Opening **Connect LocalGPT** in the PublisherStudio frontend explicitly activates the compact UDP discovery listener on port `51141`; leaving that frontend workflow releases the request, and an established LocalGPT transport can suspend discovery because the peer is already known. A selected peer can then establish the larger approved transport over TCP port `51140`. Deployments can change the activation policy through `OrganicPlugins` configuration, but the shipped policy is frontend-on-demand.

Discovery does not grant control. The user must approve the link, and each protected eye, hand, screen, spreadsheet, media, or publishing capability remains subject to its permission policy.

## Default ports

- PublisherStudio web host: `58071`
- LocalGPT 1-Wire TCP: `51140`
- LocalGPT discovery UDP: `51141`

These contracts stay separate so both applications can run side by side.

## Protocol ownership

The shared wire protocol is versioned independently and consumed from the authoritative `LocalGPT.WireProtocolVersion` package. PublisherStudio does not carry a second copy of the protocol project.

## Live organic capability synchronization

Once both frontends approve a link, PublisherStudio keeps its effective organic capability directory synchronized without requiring either application to restart or reconnect. The serializable deployed/user DX-function catalogs are watched for replacement or content changes, and peer permission changes use the same notification path. Changes are coalesced and PublisherStudio sends a fresh `CapabilityResponse` over the existing protected 1-Wire connection.

LocalGPT updates the already-linked peer registry immediately. PublisherStudio also answers explicit `CapabilityRequest` and `SkillRequest` messages, and performs a fresh comparison when `HelloAck` arrives so a catalog edit made while link approval was pending is not lost. This refreshes descriptors for functionality already executable by PublisherStudio; it does not attempt to load arbitrary new .NET code into the running process.

## Quiet standalone behavior

When LocalGPT is absent, PublisherStudio continues normally and does not bind the LocalGPT discovery UDP socket until the frontend connection workflow requests discovery. The Organic Plugins page reports that no peer is connected instead of treating the optional capability as an application failure.

## Runtime identity and small devices

PublisherStudio identity and secrets are generated at runtime; they are not shipped as a shared default credential. Small peers such as an ESP32 can participate only through an approved capability and transport profile. Discovery alone never grants an ESP32—or any other peer—permission to capture, publish, or control content.

## Traffic journal

Every 1-Wire packet PublisherStudio sends or receives is journaled: TCP envelopes in both directions (including frames that are rejected before they reach application code), the HTTP/JSON profile lookup made by **Connect by URL**, the HTTP/JSON add-on endpoint's requests and responses, and every UDP discovery datagram received while discovery listens (ignored ones included, with the reason).

The journal writes to the log category `PublisherStudio.Services.OrganicPlugins.OrganicWireTrafficJournal`:

| Level | What is written |
| --- | --- |
| Information | One line per packet: sequence, direction, transport, message type, message and correlation id, capability, peer, remote endpoint, frame size, security mode and outcome. Rejected or failed packets are written at Warning with the reason. |
| Debug | The decrypted payload of each packet, as selected by `PayloadMode`. |
| Trace | The raw frame exactly as it crossed the wire (encrypted when the peer is MFA-protected). |

The **Protocol settings and diagnostics** section of the LocalGPT AI Assist page shows the same packets live: filter by text, direction, transport or problems, pause the view, and select a packet to read its payload and raw frame.

Settings live in `OrganicPlugins:Traffic` and apply without a restart when the settings file changes:

| Setting | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Journal packets at all. |
| `PayloadMode` | `Full` | `Full` keeps the decrypted payload, `Redacted` masks values of secret-like JSON properties, `None` keeps no payload. |
| `IncludeRawFrame` | `true` | Keep the raw wire frame (logged at Trace). |
| `IncludeDiscovery` | `true` | Journal UDP discovery datagrams. |
| `JournalCapacity` | `1000` | Packets kept for the in-app view. |
| `MaximumStoredCharacters` | `262144` | Payload/raw-frame characters kept per packet in the view; `0` keeps everything. |
| `MaximumLoggedCharacters` | `0` | Payload/raw-frame characters written per log line; `0` logs everything. |
| `RedactedPropertyNames` | secret-like names | Property-name fragments masked in `Redacted` mode. |

`Full` payloads at Debug contain everything the peers exchanged, including prompts, documents and capability arguments. Keep Debug for the traffic category on machines whose log files you control, or switch `PayloadMode` to `Redacted`. Clearing the in-app view does not touch log files.

LocalGPT 7.0.1 keeps the matching journal on its side (`OneWire:Traffic`, category `LocalGPT.Services.OneWire.OneWireTrafficJournal`), so the same exchange can be read from both ends.
