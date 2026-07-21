# PublisherStudio v1.0.45 release

See `CHANGELOG-v1.0.45.md`, `docs/COMPONENT_RUNTIME.md`, and `VALIDATION.md`.

This release fixes the vertical Menu clipping issue and hardens every Component Studio runtime against stale or hidden container dimensions.

Key changes:

- Vertical Menu rows use natural heights, so all configured items display instead of only the first.
- The fix is shared by the editor, canvas, presentation HTML, and offline website HTML.
- Menu, Tile View, Splitter, and Scroll View orientation values are normalized consistently.
- A shared resize observer calls the supported DevExtreme dimension/repaint APIs after component-object resizing and visibility changes.
- Tab Panel, Multi View, and Splitter explicitly refresh nested controls when their active/available space changes.

Versions: application/package/installer `1.0.45`, publication format `1.42`, picture format `1.2`.

JavaScript syntax, Node runtime contracts, JSON/project XML parsing, CSS delimiter checks, and ZIP integrity are validated in the packaging environment. A complete `dotnet restore`/`dotnet build` and licensed DevExpress application run still require the release machine because this environment does not contain the .NET SDK or licensed DevExpress NuGet feed.
