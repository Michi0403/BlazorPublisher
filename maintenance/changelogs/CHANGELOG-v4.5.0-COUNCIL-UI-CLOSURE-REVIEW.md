# PublisherStudio 4.5.0 — AI dialogue closure, story results and global routing

Source baseline: PublisherStudio 4.4.9 source ZIP provided in this conversation.
Status: Offline source candidate. NOT compiled using .NET; Windows/DevExpress UI and live LocalGPT Council integration need testing.

## New repairs

- Mainframe AI media and selected-object popups now have explicit header close controls. Active requests are canceled locally and late results cannot repopulate a closed/reopened popup. Media Close no longer blocks on `_mediaAiBusy`.
- Media dialogue now uses the popup's available width rather than producing a large extra white area behind a narrower nested dialogue. AI content and footer retain internal scrolling, including smaller viewports.
- Story Editor AI workspace has an explicit scroll budget, while its RichEdit and footer remain accessible through the modal; does not change RichEdit's document ownership.
- Story prompt stays visible after Council request submission, with a correlated request status. Corrected proposal filtering for unique per-request `StoryEditor:<story-id>:<request-id>` targets.
- Story results are review-only: the former automatic insertion on proposal arrival was removed. A successful Council final answer without a `publisher.text.proposal.request` call is recorded as a reviewable fallback proposal; nothing is inserted without the user's click. Failed/timeout requests are surfaced in status/logs.
- Removed CanIRun.ai from PublisherStudio AI request text, buttons and now-unused localization strings. PublisherStudio no longer requests a CanIRun lookup in its native media assistance route.
- 3D AI requests now preferentially route to advertised OpenSCAD Council teams, explicitly request `publisher.openscad.generate` via the existing 1-Wire/human-approval gate, and do not label a text response or default blob as an actual user-requested model.
- Exposed a global default Council team selection in the LocalGPT connection page; offered workflow-specific overrides inside media and object assistants. Team preference uses the existing browser localStorage storage, not a new server-owned authorization policy.
- Added optional per-browser exact model-name list for Council requests. The existing OneWire 2.1.1 `ModelNames` field is populated by PublisherStudio AI bridge, Story Editor, and connection-page Council/AI Assist routes. Only explicitly selected model identifiers are passed; LocalGPT remains the authority for model inventory and actual execution. Empty list retains normal Council model selection.
- PublisherStudio.Web and PublisherStudio.Setup versions incremented 4.4.9 -> 4.5.0.

## Not claimed

- Automatic fabrication of a complex 3D mesh (for example a mammoth) is NOT implemented from an arbitrary Council text answer; OpenSCAD generation must successfully execute via a separately authorized LocalGPT capability and verified artifact workflow.
- PublisherStudio does not have a remote model catalog UI in its current wire-discovery contract; model IDs are entered explicitly and must match the LocalGPT catalog. There is no automatic model installation.
- Other unrelated DevExpress dialogs were not live-tested for sizing or closure. Source-level changes focus on the reported main AI and Story Editor dialogs.
- A completed source-level change and offline test does not verify the installed application or a Council model's behavior.

## Validation and manual acceptance

- `node --test tests/js/*.test.*`: 10/10 passed (includes AI-dialog-global-selection-4.5.0 test).
- Changes are limited to PublisherStudio; LocalGPT untouched.
- Windows: build with the repository's net10/DevExpress SDK; test in native Editor. Open 3D Blob AI and request a complex shape: expect verified tool call or clear unsupported result; never a false claim of inserted mesh. While waiting, close the popup and reopen it without refresh. Check object AI similarly. In Story Editor, generate text with a model that does NOT call the proposal function; verify a reviewable fallback answer appears and only inserts on explicit click; check a model that DOES call it. Resize window vertically and check bottom buttons/scroll. Change team/model preferences from LocalGPT connection page, navigate back to Editor, and confirm requests use selected values. Test cancellation while Council is requesting human approval and while awaiting final answer.
