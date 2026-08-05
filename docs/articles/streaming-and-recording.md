# Streaming and recording

Streaming Studio combines scenes, live inputs, chat, hotkeys, recording, and output control in one local session.

## Session flow

1. Open Streaming Studio.
2. Choose or create a profile.
3. Add page, camera, screen, window, audio, or browser sources.
4. Run a dry test.
5. Start recording or a configured output.
6. Stop the output before closing the session.

## Local capture

Browser capture is used where it fits. Windows process-loopback capture and FFmpeg services are selected only on supported platforms. Credentials and stream keys stay in protected local stores, not inside publications or interchange files.

## Preview and saved media

The preview is a live projection. The recording pipeline owns the saved bytes. A preview that needs reattachment should not corrupt a complete recording.

## When the preview turns black

First check whether the saved recording is still complete. If it is, reopen the preview or change the active source. The diagnostics bridge records browser-side failures, while expected circuit disconnects remain low-noise diagnostics.
