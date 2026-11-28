# PublisherStudio 2.7.7 source changes

- Restores the complete Media Studio record -> edit -> save/insert/replace workflow after the asynchronous recording-stop repair.
- A completed MediaRecorder Blob becomes immediately retained and usable; optional duration/poster/waveform inspection no longer blocks Save, Insert, Replace, or Download.
- Recording duration is provisionally derived from the actual recorder interval and later refined from browser metadata without discarding user edits.
- Reconnect/render attachment refreshes browser recording ownership while recording or finalization is active, so a renewed Blazor circuit receives the retained Blob state.
- Existing logging, recording stop idempotency, retained-Blob recovery, media editing, panel, publication, and export behavior remain intact.
- Wire protocol remains 2.1.1.
