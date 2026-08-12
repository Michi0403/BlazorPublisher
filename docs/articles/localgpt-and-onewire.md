# LocalGPT and 1-Wire

PublisherStudio works without LocalGPT. The 1-Wire connection is an optional local collaboration path.

## Discovery and connection

PublisherStudio listens for compact LocalGPT discovery broadcasts on UDP port `51141`. A selected peer can then establish the larger approved transport over TCP port `51140`.

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

When LocalGPT is absent, PublisherStudio continues normally. The Organic Plugins page reports that no peer is connected instead of treating the optional capability as an application failure.

## Runtime identity and small devices

PublisherStudio identity and secrets are generated at runtime; they are not shipped as a shared default credential. Small peers such as an ESP32 can participate only through an approved capability and transport profile. Discovery alone never grants an ESP32—or any other peer—permission to capture, publish, or control content.
