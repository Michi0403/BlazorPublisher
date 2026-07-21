# PublisherStudio v1.0.44 release

See `CHANGELOG-v1.0.44.md`, `docs/COMPONENT_RUNTIME.md`, `docs/ARCHITECTURE.md`, and `VALIDATION.md`.

This release fixes blank manual menus, invalid-date component failures, DevExtreme canvas movement, and connector placement/routing.

Key changes:

- Editable Menu/Context Menu item lists no longer require a publication dataset.
- Invalid date values are sanitized from static, REST, and live rows before DevExtreme formatting.
- DevExtreme components retain native click behavior but become normal movable canvas objects after the drag threshold.
- Objects support persistent custom connector points at arbitrary internal positions.
- Dropping a connector directly on an object previews and creates the attachment point.
- Selected connector paths expose draggable Bézier/route controls, and the line itself can be dragged to reposition the route.
- The Component Studio source-column compile regression and missing `initializeSignalConnectors` module export are corrected.

Versions: application/package/installer `1.0.44`, publication format `1.42`, picture format `1.2`.

JavaScript syntax, Node runtime contracts, JSON/project XML parsing, source delimiter checks, and ZIP integrity are validated in the packaging environment. A complete `dotnet restore`/`dotnet build` and licensed DevExpress application run still require the release machine because this environment does not contain the .NET SDK or licensed DevExpress NuGet feed. Headless Chromium could not be used for this revision because the available browser process is restricted in this environment.
