# PublisherStudio v1.0.90 — Organic Runtime Completion

## Startup repair

- Removed the unconditional Kestrel endpoint override when no explicit PublisherStudio port was requested. Visual Studio's launch profile at `http://127.0.0.1:5198` is now allowed to control the listener instead of the process silently moving to an unrelated dynamic port and leaving the browser on a dead address.
- Installer/explicit `--port` and `PUBLISHERSTUDIO_PORT` values remain authoritative and still configure the loopback listener.
- The runtime endpoint file is written from the actual started server address and removed only by its owning process during shutdown.

## Architecture descriptor crash repair

- Factory-based DI registrations without `ImplementationType`/`ImplementationInstance` are skipped by reflection inventory instead of constructing an invalid `ServiceArchitectureDescriptor`.
- `ServiceArchitectureDescriptor` now rejects a null implementation at its boundary and exposes non-null read-only properties.
- Registry ordering and deduplication are null-safe.
- The business/domain/API context remains available for ordinary typed registrations without making factory registrations invalid.

## Organic and media repairs

- FFmpeg capability advertisement uses the real `MediaConversionCapabilities.Available` member.
- Screenshot/media result limits now use the shared protocol maximum instead of the removed 700,000-character cap.
- The PublisherStudio → LocalGPT Council → `publisher.text.insert.propose` workflow remains visible on the Organic Plugins page and returns reviewable text proposals through the approval/result UI.
- Council team discovery, Learning Round selection, permissions, recurring screen-reader help, OpenSCAD and Spreadsheet workflows remain capability-driven and disappear/disable when LocalGPT is not connected.

## Shared protocol and versions

- Application version advanced to 1.0.90.
- LocalGPT.WireProtocolVersion 1.4 is included as a complete project and remains byte-identical to LocalGPT's authoritative copy.
- Installer, web project, JavaScript runtime metadata, streaming runtime metadata and organic-wire advertisement were advanced together.

## Regression safeguards

- Added runtime/bootstrap tests for launch-profile preservation, explicit-port behavior, endpoint ownership cleanup, null implementation descriptors, FFmpeg availability and shared protocol limits.
- Existing source-package closure safeguards continue to require `StreamingRuntimeUseCases.cs`, every project reference and all shared protocol sources.

## Validation performed in the packaging environment

- Complete `npm test` suite: passed.
- Runtime/bootstrap, architecture, C# source-safety, installer, streaming, media, OpenSCAD, automation, organic-plugin and release-gate contracts all passed.
- XML, JSON, project-reference, source-closure, protocol-mirror and archive hash checks are included with the delivery.
- Native Windows/.NET/DevExpress execution was unavailable here and is therefore not claimed.
