# PublisherStudio v1.0.90 — Remaining Work

## Target-machine verification

- Run the native Visual Studio/.NET 10 build and verify browser launch at the launch-profile address, installer launch at its explicit port, normal shutdown and repeated restart.
- Exercise DevExpress licensed browser assets and every Studio surface on the target Windows installation.

## Multi-peer and long-running recovery

- Add an explicit peer picker/routing policy when several LocalGPT instances advertise compatible capabilities.
- Persist and replay long-running organic work across a PublisherStudio process crash or machine restart; current bounded stores are process-local.

## Very-large-media transport

- Shared application limits are maximized, but chunked/resumable file transfer with backpressure is still preferable for multi-gigabyte media rather than serializing one enormous one-wire message.

## Protocol production security and transports

- Authenticated discovery, signature/encryption key enrollment and rotation remain incomplete.
- UART, SPI and MQTT adapters remain future transport implementations.

## UI hardening

- Reviewable text proposals intentionally require user acceptance before mutating a document. A richer target component/document picker can improve that interaction without removing the approval boundary.
- A full cross-theme/accessibility audit of every legacy PublisherStudio surface remains ongoing.
