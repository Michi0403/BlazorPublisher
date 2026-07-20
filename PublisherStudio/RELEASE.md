# PublisherStudio v1.0.43 release

See `CHANGELOG-v1.0.43.md`, `docs/COMPONENT_RUNTIME.md`, `docs/ARCHITECTURE.md`, and `VALIDATION.md`.

This release fixes Menu navigation and makes Component Studio data binding explicit instead of requiring guessed property names.

Key changes:

- Internal page, external URL, and no-navigation destinations coexist in editable Menu items.
- Data-driven menus navigate by stable page IDs in the editor and offline HTML exports.
- Source properties and key fields are selectable from the provisioned dataset schema.
- Live built-in data objects expose pages, document metadata, and publication objects.
- Component-specific behavior settings no longer leak Scheduler or unrelated options into Menu.
- Horizontal and vertical Menu construction is configurable.
- The duplicate DevExtreme bundle registration is removed to prevent `E0024`.

Versions: application/package/installer `1.0.43`, publication format `1.41`, picture format `1.2`.

JavaScript syntax, Node contract tests, JSON/project XML parsing, and ZIP integrity are validated in the packaging environment. A complete `dotnet restore`/`dotnet build` and licensed DevExpress application run still require the release machine because this environment does not contain the .NET SDK or licensed DevExpress NuGet feed.
