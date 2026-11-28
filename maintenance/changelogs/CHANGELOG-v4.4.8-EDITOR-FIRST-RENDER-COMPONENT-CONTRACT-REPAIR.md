# PublisherStudio 4.4.8 — Editor first-render contract repair

## Critical customer-visible regression

PublisherStudio 4.4.7 can start the server successfully but fail to render the interactive Editor: `System.InvalidOperationException: Object of type PublisherStudio.Components.Editor.InspectorPanel does not have a property matching the name OnInsertAiChat`.

## Repair

- Removed the invalid, unused `OnInsertAiChat` binding from the InspectorPanel instance in `Editor.razor`.
- Kept the valid `PublicationRibbon.OnInsertAiChat` binding and the always-visible quick-insert AI Chat action, including LocalGPT / server-configured OpenAI / no-provider behavior.
- Introduced `build/Assert-EditorComponentParameterContracts.ps1`, validating application-owned component invocation parameter names against actual `[Parameter]` declarations throughout maintained Razor files, including split code-behind components.
- Wired the guard into `Directory.Build.targets` for normal Debug and Release builds and into the direct release entrypoint before the expensive build.
- Added an independent JavaScript source-contract regression including an intentionally invalid callback as a negative control.
- Incremented the Web and installer versions from 4.4.7 to 4.4.8.

## Important

The repair addresses the confirmed `OnInsertAiChat` crash in the supplied production log. It does not assert that every possible editor interaction, installed runtime combination, or other Razor-component parameter has been smoke-tested. Full compilation, PowerShell guard execution, and a real packaged release start remain host-side release gates.
