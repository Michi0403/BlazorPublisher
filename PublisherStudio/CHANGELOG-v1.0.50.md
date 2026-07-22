# PublisherStudio 1.0.50

## Single-application streaming runtime

- Removed the separately published `PublisherStudio.MediaHost` executable and its launcher.
- Moved native capture, FFmpeg orchestration, provider delivery, recording, Chat, Now Playing, WebRTC signaling, LAN session control, and global-hotkey services directly into `PublisherStudio.Web` under `Services/StreamingRuntime`.
- The solution now contains the PublisherStudio application and InstallerConsole only. Release packaging publishes one application runtime; no `MediaHost` folder or companion process is produced.
- Kept the existing `/api/mediahost/...` route names as an internal wire-compatibility surface, but mapped them inside the main application process.
- Browser HTTP and WebSocket calls now resolve from `window.location.origin`, so dynamic loopback ports such as `127.0.0.1:49860` work without a fixed `17847` dependency.
- Runtime registries remain application singletons/hosted services, so a Blazor circuit reconnect does not own or terminate active encoders, recordings, LAN listeners, or hotkeys.

## Native devices and live sources

- `Refresh native devices` now calls the in-process discovery service directly rather than issuing an HTTP request to a second executable.
- Added clear FFmpeg-missing and operating-system permission status messages instead of exposing a loopback connection-refused exception.
- Browser webcam/camera, microphone, screen, window, and browser-tab capture continue to use browser media APIs first.
- Native capture cards/cameras, isolated Windows application audio, network media, recording, provider encoding, HLS, and RTSP use the integrated FFmpeg runtime.
- Live-source objects can now be activated by double-click or from their object context menu.
- Added live-source context commands for camera/webcam, screen, window, browser tab, native capture device, microphone, system audio, application audio, network media, and Now Playing sources, plus audio/mute and visual-fit controls.

## Context menu positioning

- Fixed nested canvas-content context menus losing page coordinates when JavaScript forwarded the event to Blazor.
- The forwarded event now preserves client, page, and screen coordinates. DevExpress therefore receives valid `PageX`/`PageY` values and can keep menus at the pointer and inside the current viewport.
- Existing object-specific context-menu commands, selection, connector, and canvas workflows remain intact.

## Tooltip stacking

- Tooltips now calculate their stacking level from the currently visible DevExpress popup/dialog overlay instead of using a permanently near-maximum z-index.
- Active tooltips close immediately before clicks, pointer presses, or context-menu opening, preventing them from covering menus or remaining above a newly opened studio.
- Tooltip positioning remains viewport-clamped and continues to support mouse hover and keyboard focus.

## FFmpeg discovery and setup

- Added shared FFmpeg discovery for an explicit configured executable, the application directory, common WinGet/Chocolatey/Scoop/Homebrew/MacPorts/Linux locations, and the operating-system `PATH`.
- Added InstallerConsole commands:
  - `--check-ffmpeg`
  - `--install-ffmpeg`
  - `--skip-ffmpeg` / `--no-ffmpeg`
- Normal installation/update now checks for FFmpeg and attempts installation with an available operating-system package manager:
  - Windows: WinGet, Chocolatey, or Scoop
  - macOS: Homebrew or MacPorts
  - Linux: APT, DNF, YUM, Zypper, Pacman, or APK
- Automatic installation reports elevation/network/package-manager failures without corrupting the PublisherStudio installation. A manually installed executable can still be selected in Streaming Studio.
- Updated the README and streaming architecture documentation with the runtime requirement and setup commands.

## Compatibility and packaging

- Provider profiles, encrypted stream/Chat secrets, publication outputs, recording plans, LAN configuration, device profiles, hotkeys, publication JSON, compositor behavior, and save/apply boundaries are preserved.
- The legacy machine-profile `MediaHostPort` value remains readable for settings compatibility but is no longer used or shown by the interface.
- Removed standalone Media Host publishing and validation from `Build-Release.ps1`.
- Installer updates remove stale `MediaHost` directories and `PublisherStudio.MediaHost*` files left by older two-process releases, preventing an obsolete companion executable from surviving the single-application migration.
- Application, InstallerConsole, npm package, and runtime capability versions are aligned to `1.0.50`.
- Publication format remains `1.45`; Picture Studio format remains `1.2`.

## Validation

- Every PublisherStudio JavaScript file passes `node --check`.
- Component, interface workflow, signal runtime, streaming runtime, and timeline contract suites pass.
- Streaming contracts verify same-origin runtime URLs, in-process registration/mapping, direct native-device discovery, FFmpeg discovery/provisioning, and absence of a standalone Media Host project/release step.
- Interface contracts verify page-coordinate forwarding, dynamic tooltip overlay stacking, and live-source activation wiring.
- A complete `dotnet restore`/`dotnet build` is not claimed in the source-packaging environment because it does not contain the .NET 10 SDK or licensed DevExpress package feed.
