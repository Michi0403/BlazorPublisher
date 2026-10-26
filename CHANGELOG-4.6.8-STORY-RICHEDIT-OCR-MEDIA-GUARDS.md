# PublisherStudio 4.6.8 — editor AI polish and truthful artifact handling

- Story Editor's Generate proposal, Refresh, insertion, replacement, dismissal and close actions now use DevExpress `DxButton` instead of raw browser buttons.
- On explicit author acceptance, Council text is HTML-encoded and deterministically converted from basic Markdown to paragraphs/headings/emphasis/lists, then inserted via DevExpress RichEdit `SubDocument.AddHtmlAsync` at the current caret or replaced interval. **No raw model HTML is trusted.**
- Story Council request transmits the current PublisherStudio RichEdit formatting contract over the existing authenticated 1-Wire channel and uses a bounded answer-token request; maintains review-before-insert and correlation.
- Empty OCR work-item results now produce a useful model/endpoint diagnostic rather than a `JsonReaderException` about zero tokens.
- Picture/video/panel Council instructions explicitly require installed specialized-model discovery, actual registered tool invocation and a verifiable media artifact; Council prose is not treated as image or video bytes. **Image generation / installation / Picture Studio editable import is not implemented by these prompt changes alone.**
- Do not infer GPT-4 Turbo behavior: the supplied two Council receipts identify Ollama gpt-oss:20b only.
- The saved Consensus step was present in the Council transcript; any missing live lane needs a separate independently verified lifecycle/UI repair.
- No Python/Node tests were added. Eight **pre-existing** Node test files had their fixed 4.6.7 version assertions replaced with dynamic semantic-version-shape checks, so future version increments do not require manual test edits. Source-only authoring, without .NET/PowerShell runtime testing.

- The existing read-only `publisher.documentation.profile` 1-Wire work result now includes the **live PublisherStudio Story Editor formatting contract**. Story requests ask the selected Council to inspect this advertised capability before drafting, with the in-request contract as a fallback. No new capability key or unauthorized write route is introduced. The model may still decline/fail to call the tool; execution is not guaranteed by prompt text.

- Story Editor Code workspace command buttons (close, format, toggle comments, insert at caret) also use DxButton; the selected-text replacement path prepares encoded RichEdit HTML before removing the previous text.
