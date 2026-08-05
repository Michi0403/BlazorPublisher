namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported open scad parameter type values.
/// </summary>
public enum OpenScadParameterType
{
    Number, Integer, Boolean, String, Vector2, Vector3, Vector4, Matrix4, Points2D, Faces, Expression, FilePath
}

/// <summary>
/// Lists supported open scad node category values.
/// </summary>
public enum OpenScadNodeCategory
{
    Primitive2D, Primitive3D, Transform, BooleanOperation, Extrusion, Projection, Import, Utility, Custom
}

/// <summary>
/// Lists supported open scad code part kind values.
/// </summary>
public enum OpenScadCodePartKind
{
    Variable, Function, Module, Raw
}

/// <summary>
/// Lists supported open scad animation property values.
/// </summary>
public enum OpenScadAnimationProperty
{
    Translate, Rotate, Scale, Resize, ColorAlpha, Parameter
}

/// <summary>
/// Lists supported open scad animation easing values.
/// </summary>
public enum OpenScadAnimationEasing
{
    Linear, EaseIn, EaseOut, EaseInOut, SmoothStep, SineInOut
}

/// <summary>
/// Represents an open scad parameter definition.
/// </summary>
public sealed class OpenScadParameterDefinition
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets type.
    /// </summary>
    public OpenScadParameterType Type { get; set; }
    /// <summary>
    /// Gets or sets required.
    /// </summary>
    public bool Required { get; set; }
    /// <summary>
    /// Gets or sets default expression.
    /// </summary>
    public string DefaultExpression { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets minimum.
    /// </summary>
    public double? Minimum { get; set; }
    /// <summary>
    /// Gets or sets maximum.
    /// </summary>
    public double? Maximum { get; set; }
}

/// <summary>
/// Represents an open scad node definition.
/// </summary>
public sealed class OpenScadNodeDefinition
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets category.
    /// </summary>
    public OpenScadNodeCategory Category { get; set; }
    /// <summary>
    /// Gets or sets accepts children.
    /// </summary>
    public bool AcceptsChildren { get; set; }
    /// <summary>
    /// Gets or sets minimum children.
    /// </summary>
    public int MinimumChildren { get; set; }
    /// <summary>
    /// Gets or sets maximum children.
    /// </summary>
    public int? MaximumChildren { get; set; }
    /// <summary>
    /// Gets or sets native export compatible.
    /// </summary>
    public bool NativeExportCompatible { get; set; } = true;
    /// <summary>
    /// Gets or sets export note.
    /// </summary>
    public string ExportNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameters.
    /// </summary>
    public List<OpenScadParameterDefinition> Parameters { get; set; } = [];
}

/// <summary>
/// Represents an open scad value.
/// </summary>
public sealed class OpenScadValue
{
    /// <summary>
    /// Gets or sets type.
    /// </summary>
    public OpenScadParameterType Type { get; set; } = OpenScadParameterType.Expression;
    /// <summary>
    /// Gets or sets number.
    /// </summary>
    public double Number { get; set; }
    /// <summary>
    /// Gets or sets integer.
    /// </summary>
    public int Integer { get; set; }
    /// <summary>
    /// Gets or sets boolean.
    /// </summary>
    public bool Boolean { get; set; }
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets vector.
    /// </summary>
    public List<double> Vector { get; set; } = [];
    /// <summary>
    /// Gets or sets matrix.
    /// </summary>
    public List<List<double>> Matrix { get; set; } = [];
    /// <summary>
    /// Gets or sets points.
    /// </summary>
    public List<List<double>> Points { get; set; } = [];
    /// <summary>
    /// Gets or sets faces.
    /// </summary>
    public List<List<int>> Faces { get; set; } = [];

}

/// <summary>
/// Represents an open scad node.
/// </summary>
public sealed class OpenScadNode
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "OpenSCAD part";
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = "cube";
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets parameters.
    /// </summary>
    public Dictionary<string, OpenScadValue> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets or sets children.
    /// </summary>
    public List<OpenScadNode> Children { get; set; } = [];
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents an open scad code part.
/// </summary>
public sealed class OpenScadCodePart
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "OpenSCAD code part";
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public OpenScadCodePartKind Kind { get; set; } = OpenScadCodePartKind.Module;
    /// <summary>
    /// Gets or sets code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents an open scad animation track.
/// </summary>
public sealed class OpenScadAnimationTrack
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Part animation";
    /// <summary>
    /// Gets or sets target node identifier.
    /// </summary>
    public Guid TargetNodeId { get; set; }
    /// <summary>
    /// Gets or sets property.
    /// </summary>
    public OpenScadAnimationProperty Property { get; set; }
    /// <summary>
    /// Gets or sets parameter name.
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets from.
    /// </summary>
    public OpenScadValue From { get; set; } = new() { Type = OpenScadParameterType.Vector3, Vector = [0, 0, 0] };
    /// <summary>
    /// Gets or sets to.
    /// </summary>
    public OpenScadValue To { get; set; } = new() { Type = OpenScadParameterType.Vector3, Vector = [0, 0, 0] };
    /// <summary>
    /// Gets or sets start.
    /// </summary>
    public double Start { get; set; }
    /// <summary>
    /// Gets or sets end.
    /// </summary>
    public double End { get; set; } = 1;
    /// <summary>
    /// Gets or sets easing.
    /// </summary>
    public OpenScadAnimationEasing Easing { get; set; } = OpenScadAnimationEasing.SmoothStep;
    /// <summary>
    /// Gets or sets loop.
    /// </summary>
    public bool Loop { get; set; }
    /// <summary>
    /// Gets or sets ping pong.
    /// </summary>
    public bool PingPong { get; set; }
}

/// <summary>
/// Represents an open scad document.
/// </summary>
public sealed class OpenScadDocument
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "OpenSCAD model";
    /// <summary>
    /// Gets or sets format version.
    /// </summary>
    public string FormatVersion { get; set; } = "1.0";
    /// <summary>
    /// Gets or sets facets.
    /// </summary>
    public int Facets { get; set; } = 48;
    /// <summary>
    /// Gets or sets includes.
    /// </summary>
    public List<string> Includes { get; set; } = [];
    /// <summary>
    /// Gets or sets uses.
    /// </summary>
    public List<string> Uses { get; set; } = [];
    /// <summary>
    /// Gets or sets code parts.
    /// </summary>
    public List<OpenScadCodePart> CodeParts { get; set; } = [];
    /// <summary>
    /// Gets or sets roots.
    /// </summary>
    public List<OpenScadNode> Roots { get; set; } = [];
    /// <summary>
    /// Gets or sets animations.
    /// </summary>
    public List<OpenScadAnimationTrack> Animations { get; set; } = [];
    /// <summary>
    /// Gets or sets metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents an open scad validation issue.
/// </summary>
public sealed record OpenScadValidationIssue(string Code, string Message, Guid? NodeId = null, InterchangeIssueSeverity Severity = InterchangeIssueSeverity.Warning);

/// <summary>
/// Represents an open scad validation result.
/// </summary>
public sealed class OpenScadValidationResult
{
    /// <summary>
    /// Gets is valid.
    /// </summary>
    public bool IsValid => Issues.All(issue => issue.Severity != InterchangeIssueSeverity.Loss);
    /// <summary>
    /// Gets or sets issues.
    /// </summary>
    public List<OpenScadValidationIssue> Issues { get; set; } = [];
}

/// <summary>
/// Represents an open scad generation result.
/// </summary>
public sealed class OpenScadGenerationResult
{
    /// <summary>
    /// Gets or sets script.
    /// </summary>
    public string Script { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets validation.
    /// </summary>
    public OpenScadValidationResult Validation { get; set; } = new();
    /// <summary>
    /// Gets or sets uses animation.
    /// </summary>
    public bool UsesAnimation { get; set; }
    /// <summary>
    /// Gets or sets requires native render.
    /// </summary>
    public bool RequiresNativeRender { get; set; } = true;
    /// <summary>
    /// Gets or sets suggested exports.
    /// </summary>
    public List<string> SuggestedExports { get; set; } = ["scad", "stl", "3mf", "off", "amf", "csg", "dxf", "svg", "png"];
}
