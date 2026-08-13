namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported open OpenSCAD parameter type values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OpenScadParameterType
{
    Number, Integer, Boolean, String, Vector2, Vector3, Vector4, Matrix4, Points2D, Faces, Expression, FilePath
}

/// <summary>
/// Defines the supported open OpenSCAD node category values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OpenScadNodeCategory
{
    Primitive2D, Primitive3D, Transform, BooleanOperation, Extrusion, Projection, Import, Utility, Custom
}

/// <summary>
/// Defines the supported open OpenSCAD code part kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OpenScadCodePartKind
{
    Variable, Function, Module, Raw
}

/// <summary>
/// Defines the supported open OpenSCAD animation property values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OpenScadAnimationProperty
{
    Translate, Rotate, Scale, Resize, ColorAlpha, Parameter
}

/// <summary>
/// Defines the supported open OpenSCAD animation easing values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OpenScadAnimationEasing
{
    Linear, EaseIn, EaseOut, EaseInOut, SmoothStep, SineInOut
}

/// <summary>
/// Represents an open OpenSCAD parameter definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OpenScadParameterDefinition
{
    /// <summary>
    /// Gets or sets the name value that forms part of the open OpenSCAD parameter definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the open OpenSCAD parameter definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the type value that forms part of the open OpenSCAD parameter definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The type value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public OpenScadParameterType Type { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the value is required applies to the open OpenSCAD parameter definition state.
    /// </summary>
    /// <value>The required value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public bool Required { get; set; }
    /// <summary>
    /// Gets or sets the default expression value that forms part of the open OpenSCAD parameter definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default expression value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public string DefaultExpression { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the open OpenSCAD parameter definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the minimum value that forms part of the open OpenSCAD parameter definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public double? Minimum { get; set; }
    /// <summary>
    /// Gets or sets the maximum value that forms part of the open OpenSCAD parameter definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum value exposed by <see cref="OpenScadParameterDefinition"/>.</value>
    public double? Maximum { get; set; }
}

/// <summary>
/// Represents an open OpenSCAD node definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OpenScadNodeDefinition
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the open OpenSCAD node definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the open OpenSCAD node definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the category value that forms part of the open OpenSCAD node definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The category value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public OpenScadNodeCategory Category { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether accepts children applies to the open OpenSCAD node definition state.
    /// </summary>
    /// <value>The accepts children value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public bool AcceptsChildren { get; set; }
    /// <summary>
    /// Gets or sets the minimum children value that forms part of the open OpenSCAD node definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum children value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public int MinimumChildren { get; set; }
    /// <summary>
    /// Gets or sets the maximum children value that forms part of the open OpenSCAD node definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum children value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public int? MaximumChildren { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether native export compatible applies to the open OpenSCAD node definition state.
    /// </summary>
    /// <value>The native export compatible value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public bool NativeExportCompatible { get; set; } = true;
    /// <summary>
    /// Gets or sets the export note value that forms part of the open OpenSCAD node definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The export note value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public string ExportNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameters collection maintained or exposed by this open OpenSCAD node definition instance for downstream processing.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="OpenScadNodeDefinition"/>.</value>
    public List<OpenScadParameterDefinition> Parameters { get; set; } = [];
}

/// <summary>
/// Represents an open OpenSCAD value application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OpenScadValue
{
    /// <summary>
    /// Gets or sets the type value that forms part of the open OpenSCAD value state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The type value exposed by <see cref="OpenScadValue"/>.</value>
    public OpenScadParameterType Type { get; set; } = OpenScadParameterType.Expression;
    /// <summary>
    /// Gets or sets the number value that forms part of the open OpenSCAD value state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The number value exposed by <see cref="OpenScadValue"/>.</value>
    public double Number { get; set; }
    /// <summary>
    /// Gets or sets the integer value that forms part of the open OpenSCAD value state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The integer value exposed by <see cref="OpenScadValue"/>.</value>
    public int Integer { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether boolean applies to the open OpenSCAD value state.
    /// </summary>
    /// <value>The boolean value exposed by <see cref="OpenScadValue"/>.</value>
    public bool Boolean { get; set; }
    /// <summary>
    /// Gets or sets the text value that forms part of the open OpenSCAD value state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="OpenScadValue"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the vector collection maintained or exposed by this open OpenSCAD value instance for downstream processing.
    /// </summary>
    /// <value>The vector value exposed by <see cref="OpenScadValue"/>.</value>
    public List<double> Vector { get; set; } = [];
    /// <summary>
    /// Gets or sets the matrix collection maintained or exposed by this open OpenSCAD value instance for downstream processing.
    /// </summary>
    /// <value>The matrix value exposed by <see cref="OpenScadValue"/>.</value>
    public List<List<double>> Matrix { get; set; } = [];
    /// <summary>
    /// Gets or sets the points collection maintained or exposed by this open OpenSCAD value instance for downstream processing.
    /// </summary>
    /// <value>The points value exposed by <see cref="OpenScadValue"/>.</value>
    public List<List<double>> Points { get; set; } = [];
    /// <summary>
    /// Gets or sets the faces collection maintained or exposed by this open OpenSCAD value instance for downstream processing.
    /// </summary>
    /// <value>The faces value exposed by <see cref="OpenScadValue"/>.</value>
    public List<List<int>> Faces { get; set; } = [];

}

/// <summary>
/// Represents an open OpenSCAD node application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OpenScadNode
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this open OpenSCAD node instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OpenScadNode"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the open OpenSCAD node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OpenScadNode"/>.</value>
    public string Name { get; set; } = "OpenSCAD part";
    /// <summary>
    /// Gets or sets the kind value that forms part of the open OpenSCAD node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="OpenScadNode"/>.</value>
    public string Kind { get; set; } = "cube";
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the open OpenSCAD node state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="OpenScadNode"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the parameters collection maintained or exposed by this open OpenSCAD node instance for downstream processing.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="OpenScadNode"/>.</value>
    public Dictionary<string, OpenScadValue> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets or sets the children collection maintained or exposed by this open OpenSCAD node instance for downstream processing.
    /// </summary>
    /// <value>The children value exposed by <see cref="OpenScadNode"/>.</value>
    public List<OpenScadNode> Children { get; set; } = [];
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this open OpenSCAD node instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="OpenScadNode"/>.</value>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents an open OpenSCAD code part application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OpenScadCodePart
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this open OpenSCAD code part instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OpenScadCodePart"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the open OpenSCAD code part state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OpenScadCodePart"/>.</value>
    public string Name { get; set; } = "OpenSCAD code part";
    /// <summary>
    /// Gets or sets the kind value that forms part of the open OpenSCAD code part state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="OpenScadCodePart"/>.</value>
    public OpenScadCodePartKind Kind { get; set; } = OpenScadCodePartKind.Module;
    /// <summary>
    /// Gets or sets the code value that forms part of the open OpenSCAD code part state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The code value exposed by <see cref="OpenScadCodePart"/>.</value>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the open OpenSCAD code part state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="OpenScadCodePart"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this open OpenSCAD code part instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="OpenScadCodePart"/>.</value>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents an open OpenSCAD animation track application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OpenScadAnimationTrack
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this open OpenSCAD animation track instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public string Name { get; set; } = "Part animation";
    /// <summary>
    /// Gets or sets the stable target node identifier used to identify or correlate this open OpenSCAD animation track instance with related application state.
    /// </summary>
    /// <value>The target node identifier value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public Guid TargetNodeId { get; set; }
    /// <summary>
    /// Gets or sets the property value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The property value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public OpenScadAnimationProperty Property { get; set; }
    /// <summary>
    /// Gets or sets the parameter name value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter name value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public string ParameterName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the from value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The from value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public OpenScadValue From { get; set; } = new() { Type = OpenScadParameterType.Vector3, Vector = [0, 0, 0] };
    /// <summary>
    /// Gets or sets the to value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The to value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public OpenScadValue To { get; set; } = new() { Type = OpenScadParameterType.Vector3, Vector = [0, 0, 0] };
    /// <summary>
    /// Gets or sets the start value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public double Start { get; set; }
    /// <summary>
    /// Gets or sets the end value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The end value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public double End { get; set; } = 1;
    /// <summary>
    /// Gets or sets the easing value that forms part of the open OpenSCAD animation track state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The easing value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public OpenScadAnimationEasing Easing { get; set; } = OpenScadAnimationEasing.SmoothStep;
    /// <summary>
    /// Gets or sets a value indicating whether loop applies to the open OpenSCAD animation track state.
    /// </summary>
    /// <value>The loop value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public bool Loop { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether ping pong applies to the open OpenSCAD animation track state.
    /// </summary>
    /// <value>The ping pong value exposed by <see cref="OpenScadAnimationTrack"/>.</value>
    public bool PingPong { get; set; }
}

/// <summary>
/// Represents open OpenSCAD state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OpenScadDocument
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this open OpenSCAD instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OpenScadDocument"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the open OpenSCAD state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OpenScadDocument"/>.</value>
    public string Name { get; set; } = "OpenSCAD model";
    /// <summary>
    /// Gets or sets the format version value that forms part of the open OpenSCAD state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format version value exposed by <see cref="OpenScadDocument"/>.</value>
    public string FormatVersion { get; set; } = "1.0";
    /// <summary>
    /// Gets or sets the facets value that forms part of the open OpenSCAD state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The facets value exposed by <see cref="OpenScadDocument"/>.</value>
    public int Facets { get; set; } = 48;
    /// <summary>
    /// Gets or sets the includes collection maintained or exposed by this open OpenSCAD instance for downstream processing.
    /// </summary>
    /// <value>The includes value exposed by <see cref="OpenScadDocument"/>.</value>
    public List<string> Includes { get; set; } = [];
    /// <summary>
    /// Gets or sets the uses collection maintained or exposed by this open OpenSCAD instance for downstream processing.
    /// </summary>
    /// <value>The uses value exposed by <see cref="OpenScadDocument"/>.</value>
    public List<string> Uses { get; set; } = [];
    /// <summary>
    /// Gets or sets the code parts collection maintained or exposed by this open OpenSCAD instance for downstream processing.
    /// </summary>
    /// <value>The code parts value exposed by <see cref="OpenScadDocument"/>.</value>
    public List<OpenScadCodePart> CodeParts { get; set; } = [];
    /// <summary>
    /// Gets or sets the roots collection maintained or exposed by this open OpenSCAD instance for downstream processing.
    /// </summary>
    /// <value>The roots value exposed by <see cref="OpenScadDocument"/>.</value>
    public List<OpenScadNode> Roots { get; set; } = [];
    /// <summary>
    /// Gets or sets the animations collection maintained or exposed by this open OpenSCAD instance for downstream processing.
    /// </summary>
    /// <value>The animations value exposed by <see cref="OpenScadDocument"/>.</value>
    public List<OpenScadAnimationTrack> Animations { get; set; } = [];
    /// <summary>
    /// Gets or sets the metadata collection maintained or exposed by this open OpenSCAD instance for downstream processing.
    /// </summary>
    /// <value>The metadata value exposed by <see cref="OpenScadDocument"/>.</value>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents an open OpenSCAD validation issue application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Code">Code value supplied to the open OpenSCAD validation issue operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the open OpenSCAD validation issue operation and used when producing its result.</param>
/// <param name="NodeId">Identifier of the node to use for this operation.</param>
/// <param name="Severity">Interchange issue severity dependency used by the open OpenSCAD validation issue workflow to provide the corresponding application capability.</param>
public sealed record OpenScadValidationIssue(string Code, string Message, Guid? NodeId = null, InterchangeIssueSeverity Severity = InterchangeIssueSeverity.Warning);

/// <summary>
/// Represents the outcome of open OpenSCAD validation, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OpenScadValidationResult
{
    /// <summary>
    /// Gets a value indicating whether valid applies to the open OpenSCAD validation state.
    /// </summary>
    /// <value>The is valid value exposed by <see cref="OpenScadValidationResult"/>.</value>
    public bool IsValid => Issues.All(issue => issue.Severity != InterchangeIssueSeverity.Loss);
    /// <summary>
    /// Gets or sets the issues collection maintained or exposed by this open OpenSCAD validation instance for downstream processing.
    /// </summary>
    /// <value>The issues value exposed by <see cref="OpenScadValidationResult"/>.</value>
    public List<OpenScadValidationIssue> Issues { get; set; } = [];
}

/// <summary>
/// Represents the outcome of open OpenSCAD generation, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class OpenScadGenerationResult
{
    /// <summary>
    /// Gets or sets the script value that forms part of the open OpenSCAD generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The script value exposed by <see cref="OpenScadGenerationResult"/>.</value>
    public string Script { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the validation value that forms part of the open OpenSCAD generation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The validation value exposed by <see cref="OpenScadGenerationResult"/>.</value>
    public OpenScadValidationResult Validation { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether uses animation applies to the open OpenSCAD generation state.
    /// </summary>
    /// <value>The uses animation value exposed by <see cref="OpenScadGenerationResult"/>.</value>
    public bool UsesAnimation { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires native render applies to the open OpenSCAD generation state.
    /// </summary>
    /// <value>The requires native render value exposed by <see cref="OpenScadGenerationResult"/>.</value>
    public bool RequiresNativeRender { get; set; } = true;
    /// <summary>
    /// Gets or sets the suggested exports collection maintained or exposed by this open OpenSCAD generation instance for downstream processing.
    /// </summary>
    /// <value>The suggested exports value exposed by <see cref="OpenScadGenerationResult"/>.</value>
    public List<string> SuggestedExports { get; set; } = ["scad", "stl", "3mf", "off", "amf", "csg", "dxf", "svg", "png"];
}
