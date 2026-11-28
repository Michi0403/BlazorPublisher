# PublisherStudio 2.6.2 — Page effects, true picture recolor, layered/merged export

## Picture Studio color replacement

- Added non-destructive raster color replacement distinct from the existing tint overlay.
- Added `ReplaceColor` and luminosity colorization modes with source color, target color, match tolerance, and strength controls.
- Added a right-click **White / light → red** command for the reported white-logo workflow. It targets `#ffffff` and maps matching RGB pixels to red while preserving the original alpha channel/antialiasing.
- Added companion right-click commands for white/light → current tint color, luminosity colorization, and reset.
- The same colorization stage is used by the live Picture Studio renderer, merged apply, PNG/JPEG download, and raster sublayers embedded in layered SVG export. This prevents the prior failure where the editor showed an effect path but export fell back to the original white raster.

## Layered versus merged results

- Picture Studio now exposes explicit **Apply layered / Insert layered** and **Apply merged / Insert merged** actions.
- Layered apply retains the `PictureDocument` on the Mainframe image so the object can be reopened and edited non-destructively.
- Merged apply bakes visible layers/effects/recoloring into the rendered image and deliberately drops editable Picture Studio ownership.
- Picture Studio output now distinguishes **Download merged PNG**, **Download merged JPEG**, and **Download layered SVG**.
- Layered SVG carries PublisherStudio Picture Studio metadata plus editable layer structure where supported; raster/effect layers are represented by their current rendered result.
- Mainframe selected-object PNG/SVG export now asks **merged or layered** whenever the selected image still owns a Picture Studio document.

## Selected-object export bounds

- Reworked selected-object raster cropping to derive the output bounds from the alpha extent of an isolated rendered page instead of the selected element's CSS rectangle.
- This preserves transformed/painted pixels, Picture Studio contents, effects, shadows, and other visual extents that can exceed or differ from the logical frame.
- Page-wide effect layers are excluded from isolated selected-object rasterization so they cannot incorrectly expand a selected object to a full-page export.

## Paint paths

- Added path-driven variants for the raster paint tools: Brush Path, Pencil Path, Spray Path, Toothbrush Path, and Eraser Path.
- Users place/edit a path first and commit it through the existing paint/stroke model, rather than being limited to freehand brush input.
- Existing freehand paint tools and the ordinary vector Path tool remain unchanged.

## Mainframe page appearance/effect layers

- Added an actual color picker for the page background through **Page appearance & effects**.
- Added publication-native page effect layers that are separate from the existing object/component Z-order.
- Effect layers support background/overlay placement, solid/linear/radial color fills, blend modes, opacity, from/to colors, duration, delay, repeat count, easing, and optional auto-reverse.
- Auto-reverse uses alternating animation direction; existing PublisherStudio easing semantics are reused for Linear, Ease In/Out/InOut, Back Out, and Bounce Out.
- Effect layers use `pointer-events:none`, so they do not interfere with selection, dragging, component interaction, or the existing object Z-stack.
- The same effect layer model renders in Mainframe and print/export surfaces. Raster export freezes animated effects at the deterministic final state.
- Publication format is now **1.57** for persisted page-effect state.

## Localization

- Added English/German catalog coverage for the new page-effect, recolor, path, layered/merged apply, and selected-export UI.
- English and German catalogs now contain **3,119** matching unique keys with no case-insensitive duplicates.

## Picture document format

- Picture Studio format is now **1.5** for persisted color-replacement state and the expanded drawing tool set.
- Existing Picture Studio documents are normalized with safe defaults for the new fields.

## Scope protection

- PublisherStudio Web and InstallerConsole are **2.6.2**.
- LocalGPT source was not changed and no LocalGPT package is produced for this release.
- The word-processing/RichEdit/printing path discussed separately was intentionally left untouched.
- No GitHub access and no dotnet/MSBuild build were used while preparing this source package.
