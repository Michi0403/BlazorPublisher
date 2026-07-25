# PublisherStudio v1.0.80

## Streaming chat hardening

- Added explicit chat presentation modes: Auto, Interactive, ViewerOnly, and StreamOverlay.
- Auto keeps the DevExpress Chat editor for the operator but switches broadcast output to a privacy-safe read-only overlay.
- Viewer and stream surfaces never render the message composer, moderation actions, sender credentials, or private operator metadata.
- Added compact layout, message limits, platform badge, age fading, and configurable background/message opacity.
- Improved stream-facing message cards, avatars, timestamps, long-token wrapping, and bounded text handling.
- Creator / Gamer Hub presets now use StreamOverlay explicitly and disable sending.
- Mainframe canvas capture uses the same privacy-safe chat configuration as structured and standalone HTML exports.
- Added regression contracts for model persistence, export wiring, privacy boundaries, stream capture, and layout hardening.
