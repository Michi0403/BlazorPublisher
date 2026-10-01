# PublisherStudio 4.0.1 — transient UI-state ownership

## Repository-wide audit after the RichEdit race repair

- Promoted the 4.0.0 Story/Text Studio fix from a component-specific lesson into a repository architecture rule: transient caret, selection, document buffer, drag-handle and similar vendor/browser interaction state must not be continuously round-tripped through `InteractiveServer` rerenders.
- The repaired `StoryEditor` remains on one-way RichEdit document initialization, observed-only selection, selection-event render suppression and editor-generation reset; no regression to `@bind-DocumentContent` or `@bind-Selection` is allowed.
- The audit found one additional PublisherStudio control with the same server-feedback shape: Media Studio's audio `DxRangeSelector` used `RangeSelectorValueChangeMode.OnHandleMove` while feeding selected start/end values back from component state. It now commits the range on `OnHandleRelease`, leaving handle movement vendor-owned while dragging.
- Publication Timeline already used `OnHandleRelease`, so no runtime change was required there.
- PublisherStudio's existing native live-preview range controls remain on the reviewed browser coalescer. The guard requires that lifecycle to stay installed and to keep excluding RichEdit, DevExpress Blazor and DevExtreme-owned controls. For new controls, commit-on-change/handle-release remains the preferred solution unless browser-owned continuous preview is genuinely required.

## Permanent maintenance contract

- Added `build/audit_transient_ui_state_ownership.py` and build-breaking `build/Assert-TransientUiStateOwnership.ps1`.
- The audit rejects complex DevExpress editor live document/selection two-way bindings, transient DevExpress selection/caret/scroll-style two-way bindings, and `DxRangeSelector` server callbacks on every handle move.
- Observable RichEdit/HtmlEditor live-state callbacks must use a named observer whose automatic ComponentBase render is suppressed. Diagnostics include the approved repair pattern instead of suggesting delays or removal of the vendor control.
- `Directory.Build.targets` wires the guard directly after the InteractiveServer render-mode guard, and `Assert-DevExpressComponentRetention.ps1` verifies that the new guard/wiring cannot be detached silently.
- `AGENTS.md` now records the rule and the explicit commit-boundary solution for future editor, slider, drag and selection work.

No GitHub access, online repository access, `dotnet`, MSBuild, restore, build, or publish is used for this source handoff.
