# Video selection, 3D and HTML interchange — v1.0.83

## Canonical ownership

A `VideoEffectLayer` is the shared unit between Video Studio, persisted publications, Mainframe preview and HTML export. The layer owns:

- an optional source-time range;
- a source polygon and optional morph-target polygon;
- a layer kind (`BaseVideo`, `Selection2D`, `Blob3D`);
- blend, opacity and filter stack;
- morph/animation/depth/roundness parameters;
- generated OpenSCAD interchange source;
- HTML compatibility metadata.

A committed timeline range therefore does not merely move anonymous markers. It can become a named layer whose effects are evaluated only within that source-time interval.

## Interaction contract

Polygon vertices have explicit selection ownership. Pointer down and context-menu activation select the vertex before any edit command runs. The property panel and delete command use that same selected index. This removes the previous ambiguity where deletion could target a different or stale point.

Source and morph polygons are edited separately. A Blob3D layer interpolates resampled polygon points so differently sized outlines can still morph predictably. The generated OpenSCAD interchange also resamples both outlines to the same vertex count, avoiding truncated morph geometry when the two selections were drawn with different point counts.

## Shared rendering path

Video Studio and Mainframe video elements use `videoEffectRuntime.js`. Temporal activation, polygon clipping, filters and Blob3D drawing are therefore shared with single-file and structured HTML exports.

A standalone blob inserted into Mainframe is represented as an HTML canvas object with a small self-contained animation runtime. This also works when reused inside Panel Studio and exported pages.

## OpenSCAD boundary

The generated OpenSCAD source is an interchange/render artifact, not a browser renderer. It uses parametric variables, functions, a module, polygon extrusion and optional Minkowski rounding. PublisherStudio embeds the source beside the Mainframe HTML object so it remains portable.

Browser-safe canvas output is classified `CanvasRuntime`. Native OpenSCAD mesh and solid operations are classified `RenderBeforeExport`, because browsers do not execute OpenSCAD. Users still retain the animated canvas fallback in HTML.

## Deliberate limits

- The current browser object is a performant 2.5D canvas blob, not a full WebGL/OpenSCAD kernel.
- STL-to-polyhedron import from OpenMorph.NET is not copied into the web application; OpenMorph remains a separate native conversion path.
- Native OpenSCAD rendering, STL export and mesh-quality video baking require an installed native renderer or a later render-service integration.


## Syntax references

- OpenSCAD syntax overview: https://en.wikibooks.org/wiki/OpenSCAD_User_Manual/General#Introduction_to_Syntax
- OpenSCAD documentation: https://openscad.org/documentation.html

The interchange generator follows OpenSCAD's statement/module/function syntax and uses semicolon-terminated assignments and objects, braces for multi-statement module bodies, vector/list comprehensions, `polygon`, `linear_extrude`, and optional `minkowski` rounding.
