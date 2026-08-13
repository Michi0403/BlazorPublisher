using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Coordinates open OpenSCAD catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class OpenScadCatalogService : IOpenScadCatalogService
{
    /// <summary>
    /// Stores the in-memory definitions collection maintained internally by <see cref="OpenScadCatalogService"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<OpenScadNodeDefinition> _definitions;
    /// <summary>
    /// Stores the in-memory by kind collection maintained internally by <see cref="OpenScadCatalogService"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyDictionary<string, OpenScadNodeDefinition> _byKind;

    /// <summary>
    /// Initializes a new <see cref="OpenScadCatalogService"/> instance and captures the dependencies or initial state required by its open OpenSCAD catalog workflow.
    /// </summary>
    public OpenScadCatalogService()
    {
        _definitions = BuildDefinitions();
        _byKind = _definitions.ToDictionary(definition => definition.Kind, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Retrieves definitions as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OpenScadNodeDefinition> GetDefinitions() {
    try
    {
        return _definitions;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.GetDefinitions failed: {__serviceMethodException}");
        throw;
    }
}
    /// <summary>
    /// Performs find as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node definition produced by the operation.</returns>
    public OpenScadNodeDefinition? Find(string kind) {
    try
    {
        return _byKind.GetValueOrDefault(kind ?? string.Empty);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.Find failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Builds definitions as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<OpenScadNodeDefinition> BuildDefinitions()
    {
    try
    {
            var definitions = new List<OpenScadNodeDefinition>
            {
                Primitive("cube", "Cube / cuboid", OpenScadNodeCategory.Primitive3D,
                    Parameter("size", "Size", OpenScadParameterType.Vector3, "[10,10,10]", required: true),
                    Parameter("center", "Centered", OpenScadParameterType.Boolean, "false")),
                Primitive("sphere", "Sphere", OpenScadNodeCategory.Primitive3D,
                    Parameter("r", "Radius", OpenScadParameterType.Number, "10", required: true, minimum: 0),
                    Parameter("$fn", "Fragments", OpenScadParameterType.Integer, "48", minimum: 3)),
                Primitive("cylinder", "Cylinder / cone", OpenScadNodeCategory.Primitive3D,
                    Parameter("h", "Height", OpenScadParameterType.Number, "10", required: true, minimum: 0),
                    Parameter("r", "Radius", OpenScadParameterType.Number, "5", minimum: 0),
                    Parameter("r1", "Bottom radius", OpenScadParameterType.Number, "undef", minimum: 0),
                    Parameter("r2", "Top radius", OpenScadParameterType.Number, "undef", minimum: 0),
                    Parameter("center", "Centered", OpenScadParameterType.Boolean, "false"),
                    Parameter("$fn", "Fragments", OpenScadParameterType.Integer, "48", minimum: 3)),
                Primitive("polyhedron", "Polyhedron", OpenScadNodeCategory.Primitive3D,
                    Parameter("points", "Points", OpenScadParameterType.Points2D, "[]", required: true),
                    Parameter("faces", "Faces", OpenScadParameterType.Faces, "[]", required: true),
                    Parameter("convexity", "Convexity", OpenScadParameterType.Integer, "10", minimum: 1)),
                Primitive("square", "Square / rectangle", OpenScadNodeCategory.Primitive2D,
                    Parameter("size", "Size", OpenScadParameterType.Vector2, "[10,10]", required: true),
                    Parameter("center", "Centered", OpenScadParameterType.Boolean, "false")),
                Primitive("circle", "Circle", OpenScadNodeCategory.Primitive2D,
                    Parameter("r", "Radius", OpenScadParameterType.Number, "10", required: true, minimum: 0),
                    Parameter("$fn", "Fragments", OpenScadParameterType.Integer, "48", minimum: 3)),
                Primitive("polygon", "Polygon", OpenScadNodeCategory.Primitive2D,
                    Parameter("points", "Points", OpenScadParameterType.Points2D, "[]", required: true),
                    Parameter("paths", "Paths", OpenScadParameterType.Faces, "undef"),
                    Parameter("convexity", "Convexity", OpenScadParameterType.Integer, "10", minimum: 1)),
                Primitive("text", "Text / WordArt source", OpenScadNodeCategory.Primitive2D,
                    Parameter("text", "Text", OpenScadParameterType.String, "\"PublisherStudio\"", required: true),
                    Parameter("size", "Font size", OpenScadParameterType.Number, "10", minimum: 0),
                    Parameter("font", "Font", OpenScadParameterType.String, ""),
                    Parameter("halign", "Horizontal alignment", OpenScadParameterType.String, "\"center\""),
                    Parameter("valign", "Vertical alignment", OpenScadParameterType.String, "\"center\""),
                    Parameter("spacing", "Spacing", OpenScadParameterType.Number, "1", minimum: 0),
                    Parameter("direction", "Direction", OpenScadParameterType.String, "\"ltr\""),
                    Parameter("language", "Language", OpenScadParameterType.String, "\"en\""),
                    Parameter("script", "Script", OpenScadParameterType.String, "\"latin\"")),
                Wrapper("translate", "Translate", OpenScadNodeCategory.Transform, Parameter("v", "Offset", OpenScadParameterType.Vector3, "[0,0,0]", true)),
                Wrapper("rotate", "Rotate", OpenScadNodeCategory.Transform, Parameter("a", "Angle", OpenScadParameterType.Vector3, "[0,0,0]", true), Parameter("v", "Axis", OpenScadParameterType.Vector3, "undef")),
                Wrapper("scale", "Scale", OpenScadNodeCategory.Transform, Parameter("v", "Scale", OpenScadParameterType.Vector3, "[1,1,1]", true)),
                Wrapper("resize", "Resize", OpenScadNodeCategory.Transform, Parameter("newsize", "New size", OpenScadParameterType.Vector3, "[0,0,0]", true), Parameter("auto", "Auto axes", OpenScadParameterType.Expression, "[false,false,false]")),
                Wrapper("mirror", "Mirror", OpenScadNodeCategory.Transform, Parameter("v", "Normal", OpenScadParameterType.Vector3, "[1,0,0]", true)),
                Wrapper("multmatrix", "Matrix transform", OpenScadNodeCategory.Transform, Parameter("m", "4×4 matrix", OpenScadParameterType.Matrix4, "[[1,0,0,0],[0,1,0,0],[0,0,1,0],[0,0,0,1]]", true)),
                Wrapper("color", "Color", OpenScadNodeCategory.Transform, Parameter("c", "Color", OpenScadParameterType.Vector4, "[0.1,0.55,0.9,1]", true), Parameter("alpha", "Alpha", OpenScadParameterType.Number, "undef", minimum: 0, maximum: 1)),
                Wrapper("offset", "2D offset", OpenScadNodeCategory.Transform, Parameter("r", "Round radius", OpenScadParameterType.Number, "undef"), Parameter("delta", "Offset", OpenScadParameterType.Number, "undef"), Parameter("chamfer", "Chamfer", OpenScadParameterType.Boolean, "false")),
                Wrapper("union", "Union", OpenScadNodeCategory.BooleanOperation, minimumChildren: 1),
                Wrapper("difference", "Difference", OpenScadNodeCategory.BooleanOperation, minimumChildren: 2),
                Wrapper("intersection", "Intersection", OpenScadNodeCategory.BooleanOperation, minimumChildren: 2),
                Wrapper("hull", "Convex hull", OpenScadNodeCategory.BooleanOperation, minimumChildren: 1),
                Wrapper("minkowski", "Minkowski sum", OpenScadNodeCategory.BooleanOperation, minimumChildren: 2, exportNote: "Can be CPU and memory intensive at high fragment counts."),
                Wrapper("render", "Force CGAL render", OpenScadNodeCategory.Utility, Parameter("convexity", "Convexity", OpenScadParameterType.Integer, "10", minimum: 1)),
                Wrapper("linear_extrude", "Linear extrusion", OpenScadNodeCategory.Extrusion,
                    Parameter("height", "Height", OpenScadParameterType.Number, "10", true, 0),
                    Parameter("center", "Centered", OpenScadParameterType.Boolean, "false"),
                    Parameter("convexity", "Convexity", OpenScadParameterType.Integer, "10", minimum: 1),
                    Parameter("twist", "Twist", OpenScadParameterType.Number, "0"),
                    Parameter("slices", "Slices", OpenScadParameterType.Integer, "20", minimum: 1),
                    Parameter("scale", "Top scale", OpenScadParameterType.Vector2, "[1,1]")),
                Wrapper("rotate_extrude", "Rotate extrusion", OpenScadNodeCategory.Extrusion,
                    Parameter("angle", "Angle", OpenScadParameterType.Number, "360"),
                    Parameter("convexity", "Convexity", OpenScadParameterType.Integer, "10", minimum: 1),
                    Parameter("$fn", "Fragments", OpenScadParameterType.Integer, "96", minimum: 3)),
                Wrapper("projection", "3D projection", OpenScadNodeCategory.Projection, Parameter("cut", "Cut at Z=0", OpenScadParameterType.Boolean, "false")),
                Primitive("import", "Import geometry", OpenScadNodeCategory.Import,
                    Parameter("file", "File", OpenScadParameterType.FilePath, "", true),
                    Parameter("convexity", "Convexity", OpenScadParameterType.Integer, "10", minimum: 1),
                    Parameter("layer", "DXF layer", OpenScadParameterType.String, "")),
                Primitive("surface", "Height-map surface", OpenScadNodeCategory.Import,
                    Parameter("file", "DAT/PNG file", OpenScadParameterType.FilePath, "", true),
                    Parameter("center", "Centered", OpenScadParameterType.Boolean, "false"),
                    Parameter("invert", "Invert", OpenScadParameterType.Boolean, "false"),
                    Parameter("convexity", "Convexity", OpenScadParameterType.Integer, "10", minimum: 1)),
                new OpenScadNodeDefinition
                {
                    Kind = "module_call", DisplayName = "Module call / assembled code part", Category = OpenScadNodeCategory.Custom,
                    AcceptsChildren = true, MinimumChildren = 0,
                    Parameters =
                    [
                        Parameter("name", "Module name", OpenScadParameterType.String, "\"part\"", true),
                        Parameter("arguments", "Arguments", OpenScadParameterType.Expression, "")
                    ],
                    NativeExportCompatible = true,
                    ExportNote = "Calls a module supplied by a document code part, include/use file or plugin."
                },
                new OpenScadNodeDefinition
                {
                    Kind = "raw", DisplayName = "Custom OpenSCAD code", Category = OpenScadNodeCategory.Custom,
                    Parameters = [Parameter("code", "Code", OpenScadParameterType.Expression, "", true)],
                    NativeExportCompatible = true,
                    ExportNote = "Custom code is preserved verbatim; validate it with the installed OpenSCAD version."
                }
            };
            return definitions.AsReadOnly();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.BuildDefinitions failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs primitive as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="category">Category value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node definition produced by the operation.</returns>
    private OpenScadNodeDefinition Primitive(string kind, string displayName, OpenScadNodeCategory category, params OpenScadParameterDefinition[] parameters) {
    try
    {
        return new() { Kind = kind, DisplayName = displayName, Category = category, Parameters = [.. parameters] };
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.Primitive failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs wrapper as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="category">Category value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node definition produced by the operation.</returns>
    private OpenScadNodeDefinition Wrapper(string kind, string displayName, OpenScadNodeCategory category, params OpenScadParameterDefinition[] parameters) {
    try
    {
        return Wrapper(kind, displayName, category, 1, null, string.Empty, parameters);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.Wrapper failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs wrapper as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="category">Category value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="minimumChildren">Minimum children value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node definition produced by the operation.</returns>
    private OpenScadNodeDefinition Wrapper(string kind, string displayName, OpenScadNodeCategory category, int minimumChildren, params OpenScadParameterDefinition[] parameters) {
    try
    {
        return Wrapper(kind, displayName, category, minimumChildren, null, string.Empty, parameters);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.Wrapper failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs wrapper as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="category">Category value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="minimumChildren">Minimum children value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="exportNote">Export note value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node definition produced by the operation.</returns>
    private OpenScadNodeDefinition Wrapper(string kind, string displayName, OpenScadNodeCategory category, int minimumChildren, string exportNote) {
    try
    {
        return Wrapper(kind, displayName, category, minimumChildren, null, exportNote, []);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.Wrapper failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs wrapper as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="category">Category value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="minimumChildren">Minimum children value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="maximumChildren">Maximum children value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="exportNote">Export note value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node definition produced by the operation.</returns>
    private OpenScadNodeDefinition Wrapper(string kind, string displayName, OpenScadNodeCategory category, int minimumChildren, int? maximumChildren, string exportNote, params OpenScadParameterDefinition[] parameters) {
    try
    {
        return new()
        {
            Kind = kind, DisplayName = displayName, Category = category, AcceptsChildren = true,
            MinimumChildren = minimumChildren, MaximumChildren = maximumChildren, Parameters = [.. parameters], ExportNote = exportNote
        };
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.Wrapper failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs parameter as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="type">Type value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="defaultExpression">Default expression value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="required">Value indicating whether required should apply to this operation.</param>
    /// <param name="minimum">Minimum value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD parameter definition produced by the operation.</returns>
    private OpenScadParameterDefinition Parameter(string name, string displayName, OpenScadParameterType type, string defaultExpression, bool required = false, double? minimum = null, double? maximum = null) {
    try
    {
        return new() { Name = name, DisplayName = displayName, Type = type, DefaultExpression = defaultExpression, Required = required, Minimum = minimum, Maximum = maximum };
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadCatalogService.Parameter failed: {__serviceMethodException}");
        throw;
    }
}
}
