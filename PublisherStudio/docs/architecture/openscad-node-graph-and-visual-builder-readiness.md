# OpenSCAD node graph and visual-builder readiness

## Open model

OpenSCAD generation is represented by public domain contracts:

- `OpenScadDocument`
- `OpenScadNode`
- `OpenScadCodePart`
- `OpenScadNodeDefinition`
- `OpenScadParameterDefinition`
- `OpenScadAnimationTrack`

The tree is data, not hard-coded rendering logic. A future visual builder can edit the same JSON-compatible graph that `/api/openscad/generate` consumes.

## Registered basic language elements

The catalog covers the built-in basic 3D primitives (`cube`, `sphere`, `cylinder`/cone, `polyhedron`), 2D primitives (`square`, `circle`, `polygon`, `text`), transforms (`translate`, `rotate`, `scale`, `resize`, `mirror`, `multmatrix`, `color`, `offset`), CSG (`union`, `difference`, `intersection`, `hull`, `minkowski`, `render`), extrusion/projection (`linear_extrude`, `rotate_extrude`, `projection`) and geometry sources (`import`, `surface`). A `raw` node preserves advanced or plugin-supplied OpenSCAD source without closing the graph. `OpenScadCodePart` stores reusable variables, functions, modules or raw source at document level, while a `module_call` node places those assembled parts into the selectable tree.

Every entry includes typed parameter metadata, defaults, required state and useful numeric ranges so a future property panel can be generated from the catalog rather than coded per figure. `IOpenScadNodeFactoryService` creates a node with typed catalog defaults and is exposed through `/api/openscad/nodes/{kind}/default`.

## Renderer extension point

`IOpenScadNodeRenderer` is a multi-registration DI strategy. A plugin can register a renderer for a new node kind without editing `OpenScadDocumentService`. The built-in primitive, wrapper and raw renderers are separate services.

## Animation

Animations target a node ID, not the whole script. Translation, rotation, scale, resize and alpha tracks wrap the selected assembled part. Generated code uses OpenSCAD's `$t` value, bounded time ranges, easing, looping and ping-pong behavior. Multiple tracks can be stacked on one part, including a `module_call` that represents a put-together code part.

Parameter animation remains an explicit extension point: the graph stores the requested parameter and range, but a node-specific renderer must decide how to inject it safely. This is marked partial in the task ledger rather than silently emitting incorrect code.

## Export compatibility

The generated `.scad` is the source interchange. Native OpenSCAD is still required to produce exact CGAL geometry such as STL, 3MF, OFF, AMF, CSG, DXF, SVG or PNG. PublisherStudio's HTML path uses its canvas representation and marks native-only geometry as requiring render-before-export.

## Visual builder boundary

A future builder should:

1. read node definitions from `IOpenScadCatalogService`;
2. create/edit `OpenScadDocument` graphs;
3. use node IDs for selection and animation tracks;
4. validate before generation;
5. add new node types through renderer registration, not switches inside the UI;
6. keep reusable module/function source in `CodeParts` and place it through `module_call` nodes.

## Reference baseline

The initial catalog follows the OpenSCAD User Manual and Language Reference for native primitives, transforms, CSG, extrusion, modules/children and `$t` animation. The repository does not copy the documentation; it keeps the generator model open so newer language elements can be registered later.
