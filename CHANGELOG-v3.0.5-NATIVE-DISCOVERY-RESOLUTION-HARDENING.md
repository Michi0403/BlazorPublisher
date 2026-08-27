# PublisherStudio 3.0.5 — Native Discovery Resolution Hardening

## Fixed / hardened

- Fully qualified `PublisherRuntimePattern.NativeDirectShowDevice` and `PublisherRuntimePattern.NativeAvFoundationDevice` at their call sites so native-device discovery does not depend on a file-level namespace import to resolve the enum.
- Kept the nullable `commentCachePath` guard introduced in 3.0.4.
- Reworked the Unix secret-file permission branch to reject Windows first and call `File.SetUnixFileMode` only on the non-Windows path, making the platform boundary explicit to analyzers.
- Preserved the existing DevExpress/Node asset preparation flow and InteractiveServer render-mode checks.

## Version

- PublisherStudio web application and installer console: `3.0.5`.
- Browser asset/cache identity and npm package identity: `3.0.5`.
- LocalGPT wire protocol remains `2.1.1`.
- Minor and patch version slots remain single-digit.
