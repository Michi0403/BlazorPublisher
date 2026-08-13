# PublisherStudio 2.6.2 picture and page-effects release

PublisherStudio 2.6.2 extends the Mainframe/Picture Studio rendering boundary without changing LocalGPT or the word-processing path.

- Adds publication-native page-wide effect layers with custom color selection, background/overlay placement, gradients, blend modes, and from/to animation settings including repeat/auto-reverse/easing.
- Adds non-destructive raster color replacement in Picture Studio, including the reported white/light-to-red workflow with transparency and antialiasing preserved.
- Adds brush, pencil, spray, toothbrush, and eraser path tools that commit through the existing paint layer model.
- Separates Picture Studio apply/export semantics into layered/editable and merged/flattened results.
- Asks for merged versus layered export when a selected Mainframe image still owns a Picture Studio layer document.
- Repairs selected-object raster bounds by cropping the isolated rendered alpha instead of the CSS frame rectangle.
- Bumps publication format to 1.57 and Picture Studio format to 1.5 for the new persisted page-effect/recolor state, with English/German localization for the new UI.

No LocalGPT package is part of this release because LocalGPT source did not change. The existing word-processing/RichEdit integration is intentionally untouched.

The owner-side Windows .NET 10 / DevExpress build remains authoritative for compilation and release publishing.
