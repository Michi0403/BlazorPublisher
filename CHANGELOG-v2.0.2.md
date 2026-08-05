# PublisherStudio 2.0.2 — historical installer repair attempt

This release attempted to recover publishing, recording preview, and optional LocalGPT discovery. Its custom preservation manifests, repair executable, legacy-root selection, and deployment transaction are **superseded** and are not maintained instructions.

The useful outcomes retained by current PublisherStudio are:

- optional LocalGPT discovery on UDP `51141` without making LocalGPT a startup dependency;
- PublisherStudio web port `58071` and 1-Wire TCP port `51140` remaining separate;
- bounded recording-preview reattachment while the saved recording path stays unchanged;
- explicit uninstall as the only operation allowed to remove an installation as a whole.

The authoritative installer contract starts with PublisherStudio 2.1.1 and is documented in `README.md`, `RELEASE.md`, `AGENTS.md`, and `docs/articles/installer-and-updates.md`.
