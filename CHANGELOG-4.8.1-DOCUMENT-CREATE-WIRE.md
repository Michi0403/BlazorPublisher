# PublisherStudio 4.8.1: Create wire (publisher.document.create, publisher.sessions.list)

Pairs with LocalGPT 7.2.0. An older LocalGPT never calls the new capabilities.

**Observed (live test):** LocalGPT could not create a spreadsheet or text document here. It only had `publisher.spreadsheet.inspect` (needs an open workbook and a session id) and `publisher.text.insert.propose` (applies only to a locally authorized target), so the model asked the human for a session id.

**Changed**
- `publisher.document.create` (confirmation required, fresh approval every time like screen capture and module invoke): request fields kind (spreadsheet or text), title, content (CSV or plain text), presentation (visible default, or silent), optional page and sessionId. After approval the owning editor circuit creates the object through the same services as AI Assist: `NormalizeCsv`, `SpreadsheetDocumentService` validation, `EditorStateService.AddSpreadsheet`, `RichTextDocumentFactory`, `AddText`/`UpdateTextDocument` (undoable). Result: object id, kind, title, page, presentation.
- `publisher.sessions.list` (read-only, no confirmation): open editor sessions with id, publication title, page count, current page and the spreadsheet/text objects (id, kind, title, page).
- New singleton `IPublisherEditorSessionRegistry`; the markup-less component `OrganicDocumentCreationBridge` in `MainLayout` registers each interactive circuit after its first render and unregisters on dispose. All state is read and changed on the owning circuit. With no open session the request fails with what the user must open; with several sessions a sessionId is required (never guessed).
- Both capabilities are in `publisher-dx-functions.json` (the advertised catalog) and in the add-on manifest; handlers are registered like the panel handlers. Approval button text "Create document"; three new localization keys in all six catalogs.
- Version 4.8.0 -> 4.8.1.

**Limits:** `visible` selects the object and page but does not open the editor dialog; `silent` keeps the user's page and selection. Text is plain text (as the existing proposal path).

**Files:** BusinessObjects/PublisherEditorObjectSnapshot.cs, PublisherEditorSessionSnapshot.cs, PublisherDocumentCreateRequest.cs, PublisherDocumentCreateResult.cs (new); Services/OrganicPlugins/IPublisherEditorSessionRegistry.cs, PublisherEditorSessionRegistry.cs, PublisherSessionsListOrganicHandler.cs, PublisherDocumentCreateOrganicHandler.cs (new), PublisherAiDocumentInsertionService.cs, OrganicCapabilityAndExecutionServices.cs (fresh-consent line); Components/OrganicPlugins/OrganicDocumentCreationBridge.razor(.cs) (new), OrganicApprovalBar.razor, Components/Layout/MainLayout.razor; PublisherStudioServiceCollectionExtensions.cs; Configuration/publisher-dx-functions.json, OrganicAddons/PublisherStudio.json; Localization/*.json; both csproj files; docs/architecture/task-ledger.md.

**Not compiled here:** no .NET SDK or PowerShell; no build, guard or runtime check was run.

**On-screen checks:** LocalGPT 7.2.0 connected; ask for a spreadsheet and a text document. Expect two approvals, the objects on the current page, undo removing them, and a clear message when no editor is open. Open two tabs: a sessionId is required.
