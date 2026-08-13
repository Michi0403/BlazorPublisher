# PublisherStudio 2.6.0 — Data, panel, media and runtime repair

## Publication data authoring

- Spreadsheet selections now infer real field types instead of freezing every selected value as text.
- The spreadsheet data-object dialog exposes an explicit per-column type choice. `Auto` keeps inference; Text, Number, Boolean and DateTime persist as explicit schema choices.
- Numeric parsing now accepts localized/grouped spreadsheet display values, currency symbols, percentages and accounting negatives. Chart/component numeric conversion uses the same parser.
- Existing publication files whose non-explicit columns were previously stored as Text are re-inferred from their rows during normalization, so older spreadsheet snapshots can recover without destructive re-import.
- Publication data validation now reports success, warning or error with row/column context and blocks invalid managed snapshots from being saved.
- Publication Data management now supports detaching source-backed rows into an editable snapshot, adding/removing/renaming columns, choosing/inferencing data types, adding/removing rows and editing field values.
- The publication file contract is now 1.56. Existing 1.55 documents remain normalized/readable; no database entity or database migration was introduced because publication datasets are document-owned JSON state.

## Components and field assignment

- Vector/component color mapping now treats actual CSS color values as colors and assigns stable palette colors to human category/group values, so assigning a color/group field produces visible output.
- The component editor labels this mapping as `Data color / group field` and explains the behavior.
- Component Studio now has Available/Phone/Tablet/Laptop/Wide preview viewport modes without rewriting authored component geometry.

## Panel Studio and rendering

- Panel Studio save callbacks and notifications are marshalled through the Blazor dispatcher, addressing `The current thread is not associated with the Dispatcher` save failures.
- Fixed-canvas panels now render through a single aspect-preserving authored canvas region instead of independently stretching nested inner and outer dimensions.
- Browser interaction and Panel Studio authoring overlays use the same canvas region for coordinates, hit testing and drops.
- Built-in panel presets now use a manageable 120 × 67.5 Mainframe frame while retaining their 160 × 90 authored canvas. KPI presets use FixedCanvas unless the author explicitly chooses Responsive behavior.
- Panel Studio now provides Design/Phone/Tablet/Laptop/Wide preview sizes, allowing preset and imported panels to be tested against different containing viewports before commit.

## Browser/runtime resilience

- html2canvas capture paths ignore DevExpress Blazor custom elements that cannot safely be cloned because of `readonly` attribute parsing, preventing the reported capture diagnostic failure.
- Spreadsheet Studio module disposal tolerates already-disconnected/disposed Blazor circuits instead of escalating `JSDisconnectedException` during shutdown.

## Media and export

- Publication media wrappers expose their authored object names to the browser drag runtime.
- Dragging images/video/audio out of PublisherStudio or standalone HTML export now supplies a named `DownloadURL`; duplicate object names such as `download.png 2` become a useful unique filename such as `download 2.png` rather than repeatedly falling back to `download.png`.
- Raster export reuses the existing compression controls: JPEG quality is configurable for single-page and multi-page output, and multi-page image ZIPs can use the structured-site ZIP compression path.

## Localization and maintenance

- Added English/German catalog entries for the new dataset maintenance, validation, preview-size, component-color and raster-compression controls.
- Added `build/audit_data_panel_media_repair.py` to keep the 2.6.0 data/panel/media architecture boundary source-auditable without requiring a .NET build.

## Version

- PublisherStudio Web: **2.6.0**
- PublisherStudio InstallerConsole: **2.6.0**
- Publication format: **1.56**
- 1-Wire protocol: unchanged
