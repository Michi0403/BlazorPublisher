# PublisherStudio 2.9.2 changelog

## Translation Editor

- Localized the Translation Editor itself through the existing `IFileLocalizationService`.
- Replaced hard-coded page title, heading, intro, navigation, culture/filter controls, Save/Use-language actions, status text and success/error notification titles with catalog-backed values and safe fallbacks.
- Culture choices now use `GetCultureDisplayName(...)` rather than exposing only raw culture codes.
- Added matching `Localization.Editor.*` entries to de-DE, en-US, es-ES, fr-FR, ja-JP and uk-UA.
- The local helper follows the repository's existing component resilience rule with method-local try/catch and structured logging.

## Release identifiers

- PublisherStudio Web and InstallerConsole versions advanced from 2.9.1 to 2.9.2.
- Existing browser/module cache identifiers were advanced to 2.9.2; no runtime design or persistence subsystem was replaced.

## Stability

- Existing InteractiveServer render boundaries are retained.
- No EF migration/schema change was introduced.
- No GitHub access and no .NET build were used while preparing this source archive.
