# PublisherStudio 4.4.0 source validation

Source baseline: `PublisherStudio-4.3.9-source.zip`.

- Confirmed `LocalGptConnection.Changed` and `OrganicResults.Changed` are synchronous `Action?` events. Event subscriptions and unsubscriptions still reference `OnOrganicStateChanged`.
- Confirmed the event adapter returns `void` and transfers the work to `ISupervisedTaskRunner`, without `async void`, unobserved tasks, or blocking coordination.
- Confirmed `OnOrganicStateChangedAsync()` returns `Task`, awaits `InsertAiProposalAsync()` on Blazor's `InvokeAsync` dispatcher, and has a method-local logged exception boundary.
- Both project versions and the application cache version are `4.4.0`, the correct rollover from `4.3.9`.
- The `build/Assert-*.ps1` files are byte-for-byte unchanged compared with the 4.3.9 baseline; no C#/Razor file other than `StoryEditor.razor` is functionally modified.
- Node tests passed: `browser-view-inspection`, `picture-studio-canvas-regression`, and `picture-studio-gif-frames`. The browser-view test logs expected permission-denied exceptions as part of its negative test.
- Source archive SHA-256/ZIP integrity checks passed.

**Not run:** .NET compiler, actual Windows PowerShell architecture gates, live DevExpress/RichEdit and 1-Wire acceptance tests. This is a source-validated, not build-verified release.
