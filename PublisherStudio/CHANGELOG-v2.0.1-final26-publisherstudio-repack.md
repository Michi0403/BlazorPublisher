# PublisherStudio 2.0.1 final26 — verified diagnostics-bridge package

This package republishes the PublisherStudio final25 component-diagnostics fix under a new archive and root-folder name so stale extraction or archive caching cannot hide the changed source.

## Source correction present

`src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor` contains an explicit catch/log/user-notification boundary in the general attachment-failure catch block:

- `Logger.LogError(...)`
- `OperationalNotifications.Error(...)`

No component-diagnostics, logging, security, 1-Wire, or runtime-value rule was weakened.

## Verification aids

- `PATCH-MANIFEST-v2.0.1-final26-publisherstudio.txt` records the exact source hash and required markers.
- `PATCH-v2.0.1-final26-publisherstudio.diff` shows the source change relative to final24.
