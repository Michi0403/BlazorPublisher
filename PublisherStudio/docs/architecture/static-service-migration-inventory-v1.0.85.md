# Static-to-service migration inventory — v1.0.85

## Purpose

This is a migration inventory, not a rewrite order. Existing working behavior remains compatible. A static member is changed only when the owning area is touched and the behavior is reusable by components, controllers, hosted services, LocalGPT/AICouncil or plugins.

## Baseline scan

A lexical scan of application C# found 466 method-like `static` declarations across 54 files. This raw number includes allowed framework entry points, extension methods, immutable domain helpers and compiler-oriented code; it is not a count of confirmed architectural violations.

Highest-value review candidates are:

| Area | Raw declarations | Migration direction |
|---|---:|---|
| `VideoProjectImportService` | 56 | format readers, path normalization and project conversion subservices |
| `SpreadsheetDocumentService` | 33 | document conversion/format services |
| `MediaConversionService` | 29 | command construction, probe parsing and path policy services |
| `OpenDocumentImportService` | 27 | package readers, XML parsing and page conversion services |
| `PublicationDataService` | 25 | data shaping, formatting and expression services |
| `PictureEditor.razor.cs` | 25 | move reusable geometry/format behavior out of the component |
| `PublicationFileService` | 21 | serializer, file-name and archive services |
| `OpenRasterImportService` | 13 | archive/XML/image decoding subservices when next touched |

## v1.0.85 boundary

New or substantially touched OpenSCAD, Video Studio interchange, automation, screenshot, code-editing, localization/path and render-capability service areas contain no private static behavior. Framework entry points and DI extension methods remain static by design.

## Required migration pattern

1. Introduce a public interface and domain request/result contracts.
2. Move reusable behavior into a service with an explicit singleton, scoped or transient lifetime.
3. Keep the existing concrete/component call path compatible.
4. Add a thin controller endpoint when the behavior is safe and useful to local API clients.
5. Add an architecture/regression contract.
6. Mark the ledger item closed only after the old static path is removed or reduced to an allowed language/framework helper.

## Next recommended slice

`VideoProjectImportService` is the largest candidate and already has strong project-import tests. Split its format detection, path resolution and adapter conversion incrementally rather than replacing the importer in one pass.
