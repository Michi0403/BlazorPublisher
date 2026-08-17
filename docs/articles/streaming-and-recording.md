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

For browser screen and camera recording, Media Studio lets you keep the selected surface at its source/native dimensions, target the configured streaming master or output size, or request a custom size. Frame rate, codec preference, video bitrate, and audio bitrate are explicit recording settings rather than hidden browser defaults. The default browser-recording video target is intentionally separate from the streaming-output bitrate so a high-resolution archival recording does not inherit a bandwidth-oriented stream setting.

Capture dimensions and frame rate are requests, not destructive resampling instructions. The browser share picker still chooses the tab, window, or display, and the browser may keep that surface's native settings when an ideal constraint cannot be satisfied. Media Studio reports the actual settings returned by the active track when recording starts.

## Preview and saved media

The preview is a live projection. The recording pipeline owns the saved bytes. A preview that needs reattachment should not corrupt a complete recording.

## When the preview turns black

First check whether the saved recording is still complete. If it is, reopen the preview or change the active source. The diagnostics bridge records browser-side failures, while expected circuit disconnects remain low-noise diagnostics.
