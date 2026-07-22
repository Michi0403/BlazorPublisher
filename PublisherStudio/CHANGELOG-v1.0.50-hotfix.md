# PublisherStudio 1.0.50 compile hotfix

## Fixed

- Removed the duplicate global `NativeMediaDeviceInfo` runtime type that shadowed `PublisherStudio.Domain.NativeMediaDeviceInfo`.
- Renamed the internal discovery result to `DiscoveredNativeMediaDeviceInfo`.
- Added an explicit mapping at the integrated streaming runtime boundary before assigning native devices to Streaming Studio.
- Fixes `CS0029` in `StreamingStudio.razor` when refreshing native devices.

## Compatibility

- No publication, profile, endpoint, capture, FFmpeg, installer, or wire-format behavior changed.
- Application version remains `1.0.50`; this archive supersedes the earlier 1.0.50 source package.
