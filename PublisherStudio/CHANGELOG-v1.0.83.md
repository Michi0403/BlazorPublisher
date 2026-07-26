# PublisherStudio v1.0.83

## Video Studio selections and markers

- Every committed non-point selection can now create or reuse its own `Selection2D` effect layer with the exact source-time range.
- A manual **Selection → new layer** action remains available when a separate layer is wanted for the same range.
- Effects, opacity, blending and frame regions on those layers are evaluated only while their temporal range is active.
- Selection-layer creation is available from ribbon, properties and context menu so the workflow remains reachable without one specific input method.

## Region-point interaction

- Clicking, dragging or opening the context menu on a polygon vertex makes that vertex the persistent selected point.
- The property panel exposes the selected point's normalized X/Y position.
- Delete removes the selected point, rather than a point inferred from stale drag state.
- Source-region and morph-target editing keep independent point collections and selection state.

## 3D blob and OpenSCAD interchange

- Video layers can be converted to `Blob3D` and can morph between a source polygon and a second polygon.
- Morph amount, animation, speed, depth and roundness are persistent layer properties.
- Browser playback uses the same canvas renderer in Video Studio, Mainframe and exported HTML.
- OpenSCAD interchange is generated from the canonical layer with `polygon`, `linear_extrude`, `minkowski`, functions, modules and special variables; source and target polygons are resampled to matching vertex counts before script generation.
- Mainframe can create a default interactive 3D blob directly from Insert > Objects or Quick Insert.
- Video Studio can insert the selected blob into Mainframe as an editable HTML object with its OpenSCAD source embedded as metadata.

## HTML export compatibility

- Layers and filters carry explicit `Native`, `CanvasRuntime` or `RenderBeforeExport` compatibility metadata.
- Native CSS effects are marked as native HTML effects.
- Chroma key, vignette, grain, color wash and 3D blob effects are marked as shared-canvas runtime effects.
- Native OpenSCAD mesh, Minkowski/boolean-solid and high-fidelity mesh output is explicitly marked **Render before HTML export**; the interactive canvas fallback is preserved.
- Blob animation is based on media time in Video Studio and `requestAnimationFrame` for standalone Mainframe/Panel objects.

## Interchange and persistence

- The canonical publication/video model persists layer kind, morph target, animation, 3D parameters, OpenSCAD source and export support notes.
- Clone, duplicate and normalization paths copy the complete new model rather than dropping 3D or export metadata.
- VideoMediaView and the HTML runtime consume the same payload fields.

## Validation

- Added `videoSelection3dInterop.test.mjs` and integrated it into `npm test`.
- All 28 repository contract suites pass.
- All JavaScript files pass `node --check`.
- A native .NET/Razor/DevExpress build still requires a machine with the .NET SDK and the configured DevExpress package source.
