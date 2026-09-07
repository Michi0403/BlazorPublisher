# PublisherStudio 3.4.6 — platform path discovery repair

## Scope

This release keeps the existing OS-specific platform service architecture and repairs path-discovery gaps only.

## FFmpeg path precedence

The common `FfmpegLocator` continues to resolve in the existing order:

1. explicit `PublisherStudio:FFmpegPath` / `PUBLISHERSTUDIO_FFMPEG`;
2. portable paths relative to `AppContext.BaseDirectory`;
3. platform-owned known installation locations;
4. inherited `PATH`.

No operating-system branching was added to the common locator.

## Unix/macOS/Linux discovery

`UnixPublisherPlatformRuntimeService` now adds common user-scoped executable locations that GUI-launched applications may not inherit through `PATH`:

- `~/.local/bin/ffmpeg`;
- `~/bin/ffmpeg`;
- `~/.nix-profile/bin/ffmpeg`;
- `/home/linuxbrew/.linuxbrew/bin/ffmpeg` on Linux.

Existing system/Homebrew/MacPorts/Snap locations remain supported. The installer-console FFmpeg probe mirrors the same user/system candidates so setup and runtime detection do not disagree.

## User guidance

Media Converter and Streaming Studio guidance now describes automatic setup, OS package-manager/manual install, portable `tools/ffmpeg`, explicit configured path/environment override, and refresh behavior without implying a single required installation scope.

## Preserved behavior

- PublisherStudio does not newly bundle FFmpeg.
- Existing Windows WinGet/Scoop/Chocolatey discovery remains unchanged.
- Existing 3.4.5 browser-chunked documentation and macOS artifact-local notarization logic remains unchanged.
- Existing InteractiveServer render-mode architecture remains unchanged.
