namespace PublisherStudio.BusinessObjects;

public enum OpenScadParameterType
{
    Number, Integer, Boolean, String, Vector2, Vector3, Vector4, Matrix4, Points2D, Faces, Expression, FilePath
}

public enum OpenScadNodeCategory
{
    Primitive2D, Primitive3D, Transform, BooleanOperation, Extrusion, Projection, Import, Utility, Custom
}

public enum OpenScadCodePartKind
{
    Variable, Function, Module, Raw
}

public enum OpenScadAnimationProperty
{
    Translate, Rotate, Scale, Resize, ColorAlpha, Parameter
}

public enum OpenScadAnimationEasing
{
    Linear, EaseIn, EaseOut, EaseInOut, SmoothStep, SineInOut
}

public sealed class OpenScadParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public OpenScadParameterType Type { get; set; }
    public bool Required { get; set; }
    public string DefaultExpression { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
}

public sealed class OpenScadNodeDefinition
{
    public string Kind { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public OpenScadNodeCategory Category { get; set; }
    public bool AcceptsChildren { get; set; }
    public int MinimumChildren { get; set; }
    public int? MaximumChildren { get; set; }
    public bool NativeExportCompatible { get; set; } = true;
    public string ExportNote { get; set; } = string.Empty;
    public List<OpenScadParameterDefinition> Parameters { get; set; } = [];
}

public sealed class OpenScadValue
{
    public OpenScadParameterType Type { get; set; } = OpenScadParameterType.Expression;
    public double Number { get; set; }
    public int Integer { get; set; }
    public bool Boolean { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<double> Vector { get; set; } = [];
    public List<List<double>> Matrix { get; set; } = [];
    public List<List<double>> Points { get; set; } = [];
    public List<List<int>> Faces { get; set; } = [];

}

public sealed class OpenScadNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "OpenSCAD part";
    public string Kind { get; set; } = "cube";
    public bool Enabled { get; set; } = true;
    public Dictionary<string, OpenScadValue> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<OpenScadNode> Children { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class OpenScadCodePart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "OpenSCAD code part";
    public OpenScadCodePartKind Kind { get; set; } = OpenScadCodePartKind.Module;
    public string Code { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class OpenScadAnimationTrack
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Part animation";
    public Guid TargetNodeId { get; set; }
    public OpenScadAnimationProperty Property { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public OpenScadValue From { get; set; } = new() { Type = OpenScadParameterType.Vector3, Vector = [0, 0, 0] };
    public OpenScadValue To { get; set; } = new() { Type = OpenScadParameterType.Vector3, Vector = [0, 0, 0] };
    public double Start { get; set; }
    public double End { get; set; } = 1;
    public OpenScadAnimationEasing Easing { get; set; } = OpenScadAnimationEasing.SmoothStep;
    public bool Loop { get; set; }
    public bool PingPong { get; set; }
}

public sealed class OpenScadDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "OpenSCAD model";
    public string FormatVersion { get; set; } = "1.0";
    public int Facets { get; set; } = 48;
    public List<string> Includes { get; set; } = [];
    public List<string> Uses { get; set; } = [];
    public List<OpenScadCodePart> CodeParts { get; set; } = [];
    public List<OpenScadNode> Roots { get; set; } = [];
    public List<OpenScadAnimationTrack> Animations { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record OpenScadValidationIssue(string Code, string Message, Guid? NodeId = null, InterchangeIssueSeverity Severity = InterchangeIssueSeverity.Warning);

public sealed class OpenScadValidationResult
{
    public bool IsValid => Issues.All(issue => issue.Severity != InterchangeIssueSeverity.Loss);
    public List<OpenScadValidationIssue> Issues { get; set; } = [];
}

public sealed class OpenScadGenerationResult
{
    public string Script { get; set; } = string.Empty;
    public OpenScadValidationResult Validation { get; set; } = new();
    public bool UsesAnimation { get; set; }
    public bool RequiresNativeRender { get; set; } = true;
    public List<string> SuggestedExports { get; set; } = ["scad", "stl", "3mf", "off", "amf", "csg", "dxf", "svg", "png"];
}
