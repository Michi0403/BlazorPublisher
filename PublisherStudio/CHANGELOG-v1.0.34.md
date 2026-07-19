# PublisherStudio v1.0.34 — Text-frame display parity and canvas polish

## Text frames

- Added the same **Natural / Clip**, **Fit whole text**, **Fill and crop**, and **Stretch to frame** commands to the text-frame canvas context menu.
- Kept the existing Text Box Tools ribbon and Properties-panel controls, so content sizing is now reachable from every normal text-object workflow.
- Text content sizing continues to apply to the publication canvas, print/PDF, image and SVG export, website export, and recorded presentation output.

## Spreadsheet frames

- Added **Show worksheet name on canvas** as an optional per-object setting. It remains enabled by default.
- Added the worksheet-name toggle to Spreadsheet Tools, the object context menu, and the Properties panel.
- The setting affects the Publisher canvas only. Print, PDF, website, and recorded presentation output remain free of the editor-only worksheet badge.

## Spreadsheet Studio ribbon

- Renamed the first custom Spreadsheet ribbon tab to **All controls** and kept it at the far-left position.
- Removed the extra custom **Open workbook** injection from DevExpress's standard Home tab.
- Retained the compact **Format** and **Cells & Data** tabs because they prevent important commands from disappearing into adaptive overflow at narrower editor widths.

## Rotated resize cursor

- Corrected the diagonal cursor mapping for CSS screen coordinates.
- Side and corner cursors now follow the actual visual resize axis at 45°, 90°, 135°, and equivalent rotations.
- Resize geometry itself is unchanged: rotated objects continue resizing in their local coordinate system with the opposite edge or corner anchored.

## File format

- Publication format marker updated to `1.34`.
- Package metadata updated to `1.0.34`.
