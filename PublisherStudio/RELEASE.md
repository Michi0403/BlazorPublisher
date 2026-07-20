# PublisherStudio v1.0.42 release

See `CHANGELOG-v1.0.42.md`, `docs/ANIMATION_EXPORT.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENT_RUNTIME.md`, and `VALIDATION.md`.

This release makes editor playback safely reversible and adds actual width/height resizing to Signal Arrow/Connector travel morphs.

Key changes:

- **Stop preview** restores the initial object and media state after regular animations and signal actions.
- Page previews include page-entry signals and begin from a clean, object-derived baseline.
- Signals can resize width and height independently while moving, scaling, rotating, and fading.
- Click/hover start conditions remain monitored after component or inner-content rerenders.
- The same reset, resize, and delegated-trigger runtime remains embedded in offline single-file HTML exports.

Versions: application/package/installer `1.0.42`, publication format `1.40`, picture format `1.2`.

JavaScript syntax, Node contract tests, JSON/project XML parsing, headless Chromium signal resize/reset and animation-restoration tests, and ZIP integrity are validated in the packaging environment. A complete `dotnet restore`/`dotnet build` and licensed DevExpress application run still require the release machine because this environment does not contain the .NET SDK or licensed DevExpress NuGet feed.
