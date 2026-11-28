# PublisherStudio 4.8.2: document creation limits are settings, not constants

4.8.1 shipped a fixed number in code (`private const int MaximumContentCharacters = 400_000`), a fixed title length (120) and two fixed timeouts (10 and 30 seconds). That is against the project's rule that tunable values are configuration.

**Changed**
- New options class `OrganicDocumentCreationOptions`, bound as `OrganicPlugins:DocumentCreation` on `OrganicPluginOptions`: `MaximumContentCharacters` (400000), `MaximumTitleCharacters` (120), `SessionDescriptionTimeoutSeconds` (10), `EditorCreationTimeoutSeconds` (30). The section is in `appsettings.json`; the class defaults apply when it is absent.
- `PublisherDocumentCreateOrganicHandler` and `PublisherEditorSessionRegistry` read these values; the constant is removed. The "content too large" error now names the setting and both numbers.
- Version 4.8.1 -> 4.8.2. Behaviour with default settings is unchanged.

**Files:** `BusinessObjects/OrganicDocumentCreationOptions.cs` (new), `BusinessObjects/OrganicPluginModels.cs`, `Services/OrganicPlugins/PublisherDocumentCreateOrganicHandler.cs`, `Services/OrganicPlugins/PublisherEditorSessionRegistry.cs`, `appsettings.json`, both project files.

**Not compiled here.** Check on screen: a create request still works; setting `MaximumContentCharacters` to a small number makes a larger request fail with a message that names the setting.
