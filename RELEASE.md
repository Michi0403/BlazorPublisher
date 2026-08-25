# PublisherStudio 2.9.8

PublisherStudio 2.9.8 is the **Story Caret, Layer Drag and Video Quality Repair** release.

It preserves the 2.9.7 working baseline while repairing the Story Editor RichEdit resize/caret feedback loop, adding canonical Mainframe layer drag/drop, making completed video recording placement explicit inside existing sequences, restoring download access for retained/current media, and removing the pathological low-frame-rate/native-quality downgrade from Video Studio rendering. The optional Edge `MediaCapabilities` recording probe now degrades cleanly to MediaRecorder support checks instead of flooding diagnostics.

The source targets .NET 10 and DevExpress/DevExtreme 25.2.9. This archive is **SOURCE-NOT-COMPILED** in the preparation environment; the user's licensed Windows build remains authoritative.

See `CHANGELOG-v2.9.8-STORY-CARET-LAYERS-VIDEO-QUALITY-REPAIR.md` and `VALIDATION-v2.9.8-source.md`.
