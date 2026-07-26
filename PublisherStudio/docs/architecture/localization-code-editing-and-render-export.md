# Localization, code editing, paths and render export

## File localization

Application translations live in `Localization/<culture>.json` and are copied to build/publish output. `IFileLocalizationService` provides culture discovery, fallback and key lookup. Initial files are supplied for `en-US`, `de-DE`, `es-ES` and `ja-JP`.

DevExpress community satellite packages for German, Spanish and Japanese are referenced for both Blazor controls and RichEdit. Existing PublisherStudio UI strings are migrated incrementally; the current four JSON files are a foundation, not a claim that every historic literal is translated.

## Code editing services

`ICodeLanguageService` describes extensions, comments, keywords and formatting characteristics for common programming and markup languages, including OpenSCAD. `ICodeFormattingService` exposes normalization, indentation, comment toggling and token analysis. The same operations are available through `/api/code` and a new Code tab in Story Editor.

RichEdit spell checking is enabled through DevExpress's built-in service. The bundled service supplies English; additional spelling dictionaries are deliberately not redistributed without a confirmed open/license-compatible dictionary source.

The current workspace is a practical textarea/token-analysis surface. Full RichEdit token-color rendering, language servers, semantic diagnostics and formatter plugins remain future extensions.

## Configurable paths

`PublisherStudio:Paths` in `appsettings.json` defines defaults for images, video, audio, documents, exports, OpenSCAD and projects. `PublicationProjectSettings` can carry project-level overrides. `/api/configuration/paths` exposes resolution and directory creation.

## Render export

The existing PNG/JPEG/SVG commands remain intact. Explicit Render commands call the same stable path while `freezeMediaForRaster` now snapshots source canvases, current video frames and accessible same-origin iframe content before the cloned page is rasterized. This fixes blank canvas/video objects without removing the existing exporter.

`IRenderExportCatalogService` describes whether each format captures live frames/effects, preserves vectors and needs render-before-export. Future exporters can add capabilities and implementations without changing publication documents.
