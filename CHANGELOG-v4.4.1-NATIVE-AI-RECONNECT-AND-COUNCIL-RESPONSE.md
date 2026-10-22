# PublisherStudio 4.4.1 — linked LocalGPT UX and native assistance

## Fixed

- When the browser previously had a successfully **linked** LocalGPT peer, prompt `Reconnect to LocalGPT?` once per browser tab session at PublisherStudio startup. Only on explicit approval does PublisherStudio activate the existing discovery service and connect to the same discovered peer; the LocalGPT-side Human Collaboration decision remains authoritative. Declining does not open a socket. A basic TCP connection is not saved as permission.
- Remember only the last linked peer ID in browser localStorage, not transport keys, credentials, or approval tokens. If browser storage is unavailable, continue without automatic connection or blocking the editor.
- Separate the ribbon's ordinary LocalGPT AI Assist button from the connection/advanced workbench. A linked user can start native Story Editor or Spreadsheet Studio from the main button and select an AI assistance action from the object context menu; the workbench is still accessible through the separate connection/settings item.
- Generic selected-object help opens a native PublisherStudio dialog with a prompt and the selected object's document/page/type context. It uses the existing permission-controlled Council bridge for read-only advice. It does not claim to apply arbitrary object edits.
- If a Council result has no `FinalAnswer`, use the most recent recorded **visible** Council step content as a human-visible reply, rather than always rejecting a completed Council as empty. Thinking content is not used as a substituted answer.
- Continue to route text and spreadsheet generation through the existing authorized proposal and native editor insertion flows; no new mutation bypass was added.

## Intact

- Picture Studio drawing, per-GIF-frame editing, animation handling, and video/media assets remain unchanged.
- Existing 1-Wire permission matrix, user-initiated insertion authorizations, undo/history, and DevExpress popup/editor components.
- PowerShell maintenance scripts and application architecture guards remain unchanged.

## Verification boundary

Source-level validation only, including the existing JS browser-view, GIF and canvas regression tests. Actual .NET compilation, WinPS architecture gates, live model output and reconnect handshakes were not available here. A successful live Council connection still requires a configured reachable provider/model. True token-by-token streaming into RichEdit/Spreadsheet Studio is not supplied by the current 1-Wire result channel.
