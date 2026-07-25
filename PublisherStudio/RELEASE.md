# PublisherStudio v1.0.71 release

See `CHANGELOG-v1.0.71.md`, `SOURCE-CHANGES-v1.0.71.txt`, `TEST-RESULTS-v1.0.71.txt`, `AGENTS.md`, ADR-010, `docs/architecture/media-gesture-editing.md`, `docs/ARCHITECTURE.md`, and `VALIDATION.md`.

This release repairs the Video Studio play-canvas regression from v1.0.70. The video fills the Studio canvas by default, the temporal selection/playback control is docked to the bottom, the selected clip remains synchronized with the source and project playheads, and the inactive frame-region layer can no longer veil or intercept normal playback.

Select time, Place playhead, Add cutline, and Frame region are explicit pointer modes in the ribbon, canvas dock, and context menu. Stretch, Fill canvas, and Fit whole are also available in both command surfaces. Video drops continue to target the selected clip and use the full-canvas timestamp marker and bounded insertion workflow.

Application and installer version is `1.0.71`. Publication format remains `1.49`, Picture Studio format remains `1.4`, and dependencies are unchanged.
