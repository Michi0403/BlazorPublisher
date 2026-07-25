# PublisherStudio v1.0.70 release

See `CHANGELOG-v1.0.70.md`, `SOURCE-CHANGES-v1.0.70.txt`, `AGENTS.md`, ADR-009, ADR-010, `docs/architecture/media-gesture-editing.md`, `docs/ARCHITECTURE.md`, and `VALIDATION.md`.

This release completes managed media composition and Video Studio temporal orchestration. Compatible files dropped onto existing page media are routed into the owning Studio instead of becoming unrelated publication objects. Picture-on-picture drops become managed Picture Studio layers; compatible video/audio drops become sequence inserts.

Video Studio now owns a timestamp/range selector over the rendered video frame. Its live, editable values stay tied to the selected project clip and can create cut boundaries, become the clip trim, copy a selected area, control playback, or constrain a dropped video's insertion timestamp. The play canvas also supports persisted Fit whole, Fill canvas and Stretch modes, with overlays recalculated against the resulting rendered frame.

Application and installer version is `1.0.70`. Publication format remains `1.49`, Picture Studio format remains `1.4`, and dependencies are unchanged.
