# PublisherStudio 2.7.6 source changes

- Restores the proven asynchronous MediaRecorder Stop contract from the earlier working Media Studio path.
- Stop is idempotent per browser recording and returns immediately instead of holding a Blazor interop call open during Blob metadata finalization.
- Browser-owned retained recording state remains recoverable after reconnect and now exposes an explicit finalizing state.
- Capture tracks are released after the final Blob is constructed, before slower metadata/poster inspection.
- Existing logging, editor, panel, media, and publication behavior remains intact.
- Wire protocol remains 2.1.1.
