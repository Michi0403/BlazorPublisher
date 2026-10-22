# PublisherStudio 4.4.0 — Story Editor asynchronous proposal event repair

## Fixed

- Fixed `CS4034` in `src/PublisherStudio.Web/Components/Editor/StoryEditor.razor` by separating the synchronous `Action`-compatible `OnOrganicStateChanged` event adapter from `OnOrganicStateChangedAsync`, a real `async Task` operation.
- Preserved `ISupervisedTaskRunner` ownership for events that cannot return `Task`, and dispatched the asynchronous work through Blazor `InvokeAsync` to protect DevExpress RichEdit and component renderer state.
- Kept matching AI proposal insertion, request correlation, UI refresh, method-local diagnostics, and exception logging intact; no `async void` or fire-and-forget task was introduced.
- Increased both `PublisherStudio.Web` and `PublisherStudio.InstallerConsole` versions from `4.3.9` to `4.4.0`, respecting the single-digit minor/patch policy. Updated application cache-query version marker accordingly; JavaScript content is unchanged.

## Unchanged

- No Picture Studio, canvas, GIF, spreadsheet, one-wire, browser-inspection, or localization logic changed.
- All existing architecture guards and their enforcement rules are identical to those shipped in 4.3.9.
- Companion LocalGPT 6.2.0 is unaffected. Its Ollama `HttpClient` ownership and commented-out session-disposal workaround are separate runtime concerns; this release does not claim they are solved.

## Verification boundary

- Static source inspection and the existing Node tests passed.
- A .NET compile, Windows PowerShell gates, and live RichEdit/LocalGPT integration were not run in this environment.
