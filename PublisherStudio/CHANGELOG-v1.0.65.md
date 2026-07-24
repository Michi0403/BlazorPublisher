# PublisherStudio 1.0.65

## Picture Studio open interchange

- Added structured SVG and SVGZ import without introducing a new NuGet, npm package, native binary or helper process.
- Imports paths, rectangles, circles, ellipses, lines, polylines, polygons, text, embedded images and `use` instances as separate editable vector layers.
- Retains source group/layer paths, including Inkscape layer labels and ordinary named SVG groups.
- Retains local definitions needed by each vector object, including gradients, patterns, masks, clipping paths, markers, filters, transforms and inherited group styling.
- Hidden source layers are preserved as hidden Picture Studio layers rather than silently discarded.
- PublisherStudio-authored SVG metadata round-trips directly back into the native Picture Studio document.
- Added layered OpenRaster (`.ora`) import using only BCL ZIP/XML support. Nested stack names, order, offsets, visibility, opacity, edit locks, common blend modes, PNG/JPEG/WebP layers and SVG/SVGZ layers are mapped into Picture Studio.
- Added explicit compatibility issues for missing, unsafe, oversized, malformed or unsupported layers instead of aborting the whole import where recovery is possible.
- Added size, decompression and path-traversal limits for SVGZ and OpenRaster package input.
- Executable SVG elements, event attributes, DTD/entity processing and online URL dependencies are removed or rejected so imports remain offline and deterministic.

## Real Path tool

- Replaced the former brush-like Path interaction with node placement.
- Click to add nodes, move the pointer to preview the next segment, and double-click or press Enter to commit.
- Hold Shift while finishing to close the path; Escape cancels the unfinished path.
- The resulting native path remains editable through the existing point list, add/remove, reverse, smooth and close controls.

## WordArt picture and video fills

- WordArt can now use Solid, Gradient, Picture or Video fill modes.
- Picture and video fills use the same live WordArt glyph/path mask, including warped and custom-path WordArt.
- Added cover/contain/stretch fitting, scale, horizontal/vertical offset and video-loop controls.
- Image fills are embedded in the publication. SVG image fills pass through the same offline sanitizer before storage.
- Video fills are embedded and preview live in the editor and interactive HTML exports.
- A representative poster frame is generated for deterministic print/PDF/static rendering; raster and SVG export paths also freeze live media through the existing export media snapshot pipeline.
- Legacy `GradientFill` publications migrate into the new canonical fill kind without changing their appearance.

## OpenDocument page import

- Added import for OpenDocument Drawing and Presentation packages (`.odg`, `.odp`) and flat XML variants (`.fodg`, `.fodp`).
- `draw:page` and presentation slides map to PublisherStudio pages using the source master-page dimensions where available.
- Text boxes map to native Story/Text frames, embedded package or inline-base64 images map to native image frames, and rectangles, ellipses and lines map to native shapes.
- ODF path, polygon and polyline objects are retained as sanitized SVG-backed Picture Studio vector content.
- Custom shapes are approximated explicitly and unsupported elements, group transforms, missing assets and compatibility losses are reported.
- Package paths, XML entity handling, entry sizes and inline assets are validated before the temporary publication is committed.

## Architecture and formats

- Added canonical `InterchangeIssue`, `PictureImportResult` and `PublicationImportResult` contracts under `Domain`.
- Format adapters live under `Services/PictureStudio/Import` and `Services/Publication/Import`; Components only select files, display results and commit validated canonical documents.
- Added ADR-007 and expanded `AGENTS.md` with dependency, package, XML/SVG security, archive-validation and loss-reporting rules.
- Added deterministic SVG, OpenRaster-stack and OpenDocument fixture documents plus a dedicated interchange contract suite.
- Publication format is now `1.48`; Picture Studio format is now `1.3`.

Application and installer version: `1.0.65`.
