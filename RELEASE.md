# PublisherStudio 2.9.9

PublisherStudio 2.9.9 is the **Video Sequence and Capture Presets Repair** release.

It preserves the 2.9.8 Story Editor caret repair, Mainframe layer dragging, recording download repair, Edge MediaCapabilities fallback and source-frame-driven video rendering. It closes the remaining repeated-recording loss path by making every completed video recording explicitly sequence-owned before another browser capture may replace the retained Blob, and adds first/between/last pre-capture placement for subsequent recordings.

Video Studio also gains standard landscape, vertical and square resolution presets, reuses Panel / Div Studio viewport presets, and supports fractional cinema/NTSC and high-refresh frame-rate choices through 240 FPS within the existing runtime policy.

See `CHANGELOG-v2.9.9-VIDEO-SEQUENCE-CAPTURE-PRESETS.md` and `VALIDATION-v2.9.9-source.md`.
