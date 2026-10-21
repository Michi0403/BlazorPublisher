# PublisherStudio 4.3.8 — AI Assist object context and native artifact insertion

## Source baseline

4.3.7 complete source ZIP. No Picture Studio, GIF, rendering, browser JavaScript, or drawing-canvas file has been changed. Existing PowerShell architecture/quality gates remain unchanged. No GitHub access or .NET build.

## Changes

- The `AI Assist selected object` ribbon command now presents write-story, create-spreadsheet, and selected-object help choices. The Page Surface native DevExpress context menu offers the same operations. The selected object remains authoritative in EditorState; navigation contains only the output-kind key, never private content.
- AI Assist includes bounded previews of the existing story/spreadsheet in its LocalGPT request context rather than relying exclusively on the name/type of the selected object.
- A new scoped, DI-owned `PublisherAiDocumentInsertionService` records a user-initiated insertion intent with a non-guessable correlation target plus document/page/element identity. Only an exact returned `publisher.text.proposal.request` proposal can consume it; ordinary/unrequested proposals remain review-only.
- The returned story is converted to a real native OpenXML TextFrame via the existing rich-text factory and `EditorStateService` undoable mutations. Spreadsheet requests create/update a native CSV-backed SpreadsheetElement using existing spreadsheet validation and HTML preview. Markdown tables are translated to CSV cells for this path.
- A document/page switch, removed/locked target, disconnected send, or unsupported AI profile prevents automatic insertion. Failed results remain reviewable, never silently dropped. 1-Wire security and LocalGPT DX function approval are not relaxed.
- Both the AI Assist page and the normal publication editor now listen for authorized results on the Blazor dispatcher; insertion occurs even if the user returns to the publication before LocalGPT completes. The outgoing Council request precisely names the target nonce and required tool result.
- Added three AI context-menu keys to all six maintained localization catalogs, including meaningful translations.
- Web and InstallerConsole versions incremented 4.3.7 → 4.3.8; a PowerShell-only source integration regression was added.

## Verification status

Reviewed C#/Razor contract and service DI wiring, localization parity and unchanged JavaScript manifests. Source-only static regression can be run with `tests/static/Assert-AiAssistIntegration.ps1`. No .NET/DevExpress compilation or Windows 1-Wire end-to-end execution is claimed. Native editing requires a successfully approved LocalGPT Council tool proposal; the system will not fabricate an AI output or bypass the approval gate.
