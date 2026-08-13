using System.Text;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Coordinates open OpenSCAD document behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="catalog">Open openscad catalog service dependency used by the open OpenSCAD document workflow to provide the corresponding application capability.</param>
/// <param name="values">Open openscad value formatter dependency used by the open OpenSCAD document workflow to provide the corresponding application capability.</param>
/// <param name="renderers">Open openscad node renderer dependency used by the open OpenSCAD document workflow to provide the corresponding application capability.</param>
public sealed class OpenScadDocumentService(
    IOpenScadCatalogService catalog,
    IOpenScadValueFormatter values,
    IEnumerable<IOpenScadNodeRenderer> renderers) : IOpenScadDocumentService
{
    /// <summary>
    /// Stores the in-memory renderers collection maintained internally by <see cref="OpenScadDocumentService"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<IOpenScadNodeRenderer> _renderers = renderers.ToList().AsReadOnly();

    /// <summary>
    /// Performs validate as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD validation result produced by the operation.</returns>
    public OpenScadValidationResult Validate(OpenScadDocument document)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(document);
            var result = new OpenScadValidationResult();
            var codePartIds = new HashSet<Guid>();
            var codePartNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in document.CodeParts.Where(part => part.Enabled))
            {
                if (!codePartIds.Add(part.Id)) result.Issues.Add(new("duplicate-code-part-id", $"Code part id {part.Id} occurs more than once.", Severity: InterchangeIssueSeverity.Loss));
                if (!string.IsNullOrWhiteSpace(part.Name) && !codePartNames.Add(part.Name)) result.Issues.Add(new("duplicate-code-part-name", $"Code part name '{part.Name}' occurs more than once."));
                if (string.IsNullOrWhiteSpace(part.Code)) result.Issues.Add(new("empty-code-part", $"Code part '{part.Name}' has no source.", Severity: InterchangeIssueSeverity.Loss));
            }
            var ids = new HashSet<Guid>();
            foreach (var node in Enumerate(document.Roots))
            {
                if (!ids.Add(node.Id))
                    result.Issues.Add(new("duplicate-node-id", $"Node id {node.Id} occurs more than once.", node.Id, InterchangeIssueSeverity.Loss));
                var definition = catalog.Find(node.Kind);
                if (definition is null && !_renderers.Any(renderer => renderer.CanRender(node)))
                {
                    result.Issues.Add(new("unknown-node-kind", $"No registered OpenSCAD node renderer handles '{node.Kind}'.", node.Id, InterchangeIssueSeverity.Loss));
                    continue;
                }
                if (definition is null) continue;
                var enabledChildren = node.Children.Count(child => child.Enabled);
                if (!definition.AcceptsChildren && enabledChildren > 0)
                    result.Issues.Add(new("unexpected-children", $"'{definition.DisplayName}' does not consume child nodes.", node.Id));
                if (definition.AcceptsChildren && enabledChildren < definition.MinimumChildren)
                    result.Issues.Add(new("missing-children", $"'{definition.DisplayName}' requires at least {definition.MinimumChildren} child node(s).", node.Id, InterchangeIssueSeverity.Loss));
                if (definition.MaximumChildren is { } maximum && enabledChildren > maximum)
                    result.Issues.Add(new("too-many-children", $"'{definition.DisplayName}' supports at most {maximum} child node(s).", node.Id, InterchangeIssueSeverity.Loss));
                foreach (var parameter in definition.Parameters.Where(parameter => parameter.Required))
                {
                    if (!node.Parameters.ContainsKey(parameter.Name) && string.IsNullOrWhiteSpace(parameter.DefaultExpression))
                        result.Issues.Add(new("missing-parameter", $"'{definition.DisplayName}' requires parameter '{parameter.Name}'.", node.Id, InterchangeIssueSeverity.Loss));
                }
                foreach (var parameter in definition.Parameters)
                {
                    if (node.Parameters.TryGetValue(parameter.Name, out var value)) ValidateParameter(result, node.Id, definition, parameter, value);
                }
            }

            var validIds = ids;
            foreach (var animation in document.Animations)
            {
                if (!validIds.Contains(animation.TargetNodeId))
                    result.Issues.Add(new("animation-target-missing", $"Animation '{animation.Name}' targets an unknown node.", animation.TargetNodeId, InterchangeIssueSeverity.Loss));
                if (animation.End <= animation.Start)
                    result.Issues.Add(new("animation-range-invalid", $"Animation '{animation.Name}' must end after it starts.", animation.TargetNodeId, InterchangeIssueSeverity.Loss));
            }
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.Validate failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs generate as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD generation result produced by the operation.</returns>
    public OpenScadGenerationResult Generate(OpenScadDocument document)
    {
    try
    {
            var validation = Validate(document);
            var builder = new StringBuilder();
            builder.AppendLine("// PublisherStudio open OpenSCAD document interchange");
            builder.AppendLine("// Node graph, catalog and renderers are service-driven for future visual-builder/plugin use.");
            builder.AppendLine($"$fn = {Math.Clamp(document.Facets, 3, 720)};");
            foreach (var include in document.Includes.Where(path => !string.IsNullOrWhiteSpace(path)))
                builder.AppendLine($"include <{include.Trim()}>;");
            foreach (var use in document.Uses.Where(path => !string.IsNullOrWhiteSpace(path)))
                builder.AppendLine($"use <{use.Trim()}>;");
            foreach (var part in document.CodeParts.Where(part => part.Enabled && !string.IsNullOrWhiteSpace(part.Code)))
            {
                builder.AppendLine();
                builder.AppendLine($"// PublisherStudio code part: {part.Name} ({part.Kind})");
                builder.AppendLine(part.Code.Trim());
            }
            if (document.Animations.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("function ps_clamp(v,a,b)=min(b,max(a,v));");
                builder.AppendLine("function ps_ease_linear(t)=t;");
                builder.AppendLine("function ps_ease_in(t)=t*t;");
                builder.AppendLine("function ps_ease_out(t)=1-(1-t)*(1-t);");
                builder.AppendLine("function ps_ease_in_out(t)=t<0.5?2*t*t:1-pow(-2*t+2,2)/2;");
                builder.AppendLine("function ps_ease_smooth(t)=t*t*(3-2*t);");
                builder.AppendLine("function ps_ease_sine(t)=-(cos(180*t)-1)/2;");
                builder.AppendLine("function ps_lerp(a,b,t)=a+(b-a)*t;");
                builder.AppendLine("function ps_lerp_v(a,b,t)=[for(i=[0:min(len(a),len(b))-1]) ps_lerp(a[i],b[i],t)];");
                builder.AppendLine("function ps_track(t,start,end,loop=false,pingpong=false)=let(raw=(t-start)/max(0.000001,end-start),wrapped=loop?(raw-floor(raw)):ps_clamp(raw,0,1)) pingpong?(wrapped<=0.5?wrapped*2:(1-wrapped)*2):wrapped;");
            }
            builder.AppendLine();
            foreach (var root in document.Roots.Where(node => node.Enabled))
                builder.AppendLine(RenderNode(document, root, 0));
            return new OpenScadGenerationResult
            {
                Script = builder.ToString(), Validation = validation,
                UsesAnimation = document.Animations.Count > 0,
                RequiresNativeRender = true
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.Generate failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Creates example document as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The open OpenSCAD document produced by the operation.</returns>
    public OpenScadDocument CreateExampleDocument()
    {
    try
    {
            var cube = new OpenScadNode
            {
                Name = "Animated cube", Kind = "cube",
                Parameters = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["size"] = new() { Type = OpenScadParameterType.Vector3, Vector = [20, 20, 20] },
                    ["center"] = new() { Type = OpenScadParameterType.Boolean, Boolean = true }
                }
            };
            var sphere = new OpenScadNode
            {
                Name = "Cutting sphere", Kind = "sphere",
                Parameters = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["r"] = new() { Type = OpenScadParameterType.Number, Number = 13 }
                }
            };
            var moduleCall = new OpenScadNode
            {
                Name = "Code-part pedestal", Kind = "module_call",
                Parameters = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = new() { Type = OpenScadParameterType.String, Text = "publisher_pedestal" },
                    ["arguments"] = new() { Type = OpenScadParameterType.Expression, Text = "24, 4" }
                }
            };
            var difference = new OpenScadNode { Name = "Assembly", Kind = "difference", Children = [cube, sphere] };
            var union = new OpenScadNode { Name = "Put-together model", Kind = "union", Children = [difference, moduleCall] };
            return new OpenScadDocument
            {
                Name = "PublisherStudio animated CSG example", Roots = [union],
                CodeParts =
                [
                    new OpenScadCodePart
                    {
                        Name = "PublisherStudio code-part example", Kind = OpenScadCodePartKind.Module,
                        Code = "module publisher_pedestal(width=24,height=4) { translate([0,0,-12]) cube([width,width,height],center=true); }"
                    }
                ],
                Animations =
                [
                    new OpenScadAnimationTrack
                    {
                        Name = "Move cube", TargetNodeId = cube.Id, Property = OpenScadAnimationProperty.Translate,
                        From = new() { Type = OpenScadParameterType.Vector3, Vector = [-20, 0, 0] },
                        To = new() { Type = OpenScadParameterType.Vector3, Vector = [20, 0, 0] },
                        Easing = OpenScadAnimationEasing.SineInOut, PingPong = true
                    }
                ]
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.CreateExampleDocument failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render node as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <param name="node">Node value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <param name="depth">Depth value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RenderNode(OpenScadDocument document, OpenScadNode node, int depth)
    {
    try
    {
            var renderer = _renderers.FirstOrDefault(candidate => candidate.CanRender(node));
            if (renderer is null) return $"{new string(' ', depth * 4)}// No renderer registered for {node.Kind}";
            var context = new OpenScadRenderContext
            {
                Values = values,
                Depth = depth,
                RenderNode = (child, childDepth) => RenderNode(document, child, childDepth)
            };
            var rendered = renderer.Render(node, context);
            var tracks = document.Animations.Where(track => track.TargetNodeId == node.Id).ToList();
            if (tracks.Count == 0) return rendered;
            var indent = new string(' ', depth * 4);
            var wrapped = rendered;
            foreach (var track in tracks.AsEnumerable().Reverse())
            {
                var amount = TrackExpression(track);
                var from = values.Format(track.From);
                var to = values.Format(track.To);
                var argument = track.Property == OpenScadAnimationProperty.Parameter
                    ? $"// Parameter animation '{track.ParameterName}' requires a custom node renderer. amount={amount}\n"
                    : track.Property switch
                    {
                        OpenScadAnimationProperty.Translate => $"translate(ps_lerp_v({from},{to},{amount})) ",
                        OpenScadAnimationProperty.Rotate => $"rotate(ps_lerp_v({from},{to},{amount})) ",
                        OpenScadAnimationProperty.Scale => $"scale(ps_lerp_v({from},{to},{amount})) ",
                        OpenScadAnimationProperty.Resize => $"resize(ps_lerp_v({from},{to},{amount})) ",
                        OpenScadAnimationProperty.ColorAlpha => $"color([0.1,0.55,0.9,ps_lerp({from},{to},{amount})]) ",
                        _ => string.Empty
                    };
                wrapped = argument.StartsWith("//", StringComparison.Ordinal)
                    ? indent + argument + wrapped
                    : $"{indent}{argument}{{\n{IndentBlock(wrapped, 1)}\n{indent}}}";
            }
            return wrapped;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.RenderNode failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Validates parameter as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <param name="nodeId">Identifier of the node to use for this operation.</param>
    /// <param name="definition">Definition value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <param name="parameter">Parameter value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    private void ValidateParameter(OpenScadValidationResult result, Guid nodeId, OpenScadNodeDefinition definition, OpenScadParameterDefinition parameter, OpenScadValue value)
    {
    try
    {
            if (value.Type != parameter.Type && value.Type != OpenScadParameterType.Expression)
                result.Issues.Add(new("parameter-type", $"'{definition.DisplayName}.{parameter.Name}' expects {parameter.Type} but received {value.Type}.", nodeId));
            var numeric = value.Type switch
            {
                OpenScadParameterType.Number => value.Number,
                OpenScadParameterType.Integer => value.Integer,
                _ => (double?)null
            };
            if (numeric is { } number)
            {
                if (!double.IsFinite(number)) result.Issues.Add(new("parameter-nonfinite", $"'{definition.DisplayName}.{parameter.Name}' must be finite.", nodeId, InterchangeIssueSeverity.Loss));
                if (parameter.Minimum is { } minimum && number < minimum) result.Issues.Add(new("parameter-minimum", $"'{definition.DisplayName}.{parameter.Name}' is below {minimum}.", nodeId));
                if (parameter.Maximum is { } maximum && number > maximum) result.Issues.Add(new("parameter-maximum", $"'{definition.DisplayName}.{parameter.Name}' is above {maximum}.", nodeId));
            }
            var expectedVectorLength = parameter.Type switch
            {
                OpenScadParameterType.Vector2 => 2, OpenScadParameterType.Vector3 => 3, OpenScadParameterType.Vector4 => 4, _ => 0
            };
            if (expectedVectorLength > 0 && value.Type != OpenScadParameterType.Expression && value.Vector.Count != expectedVectorLength)
                result.Issues.Add(new("parameter-vector-size", $"'{definition.DisplayName}.{parameter.Name}' expects {expectedVectorLength} values.", nodeId, InterchangeIssueSeverity.Loss));
            if (parameter.Type == OpenScadParameterType.Matrix4 && value.Type != OpenScadParameterType.Expression && (value.Matrix.Count != 4 || value.Matrix.Any(row => row.Count != 4)))
                result.Issues.Add(new("parameter-matrix-size", $"'{definition.DisplayName}.{parameter.Name}' expects a 4x4 matrix.", nodeId, InterchangeIssueSeverity.Loss));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.ValidateParameter failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs track expression as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="track">Track value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string TrackExpression(OpenScadAnimationTrack track)
    {
    try
    {
            var raw = $"ps_track($t,{Number(track.Start)},{Number(track.End)},{(track.Loop ? "true" : "false")},{(track.PingPong ? "true" : "false")})";
            return track.Easing switch
            {
                OpenScadAnimationEasing.EaseIn => $"ps_ease_in({raw})",
                OpenScadAnimationEasing.EaseOut => $"ps_ease_out({raw})",
                OpenScadAnimationEasing.EaseInOut => $"ps_ease_in_out({raw})",
                OpenScadAnimationEasing.SmoothStep => $"ps_ease_smooth({raw})",
                OpenScadAnimationEasing.SineInOut => $"ps_ease_sine({raw})",
                _ => raw
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.TrackExpression failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs indent block as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <param name="levels">Levels value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string IndentBlock(string source, int levels)
    {
    try
    {
            var prefix = new string(' ', levels * 4);
            return string.Join(Environment.NewLine, source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => prefix + line));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.IndentBlock failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs number as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Number(double value) {
    try
    {
        return (double.IsFinite(value) ? value : 0).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadDocumentService.Number failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs enumerate as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="nodes">Open openscad node dependency used by the open OpenSCAD document workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<OpenScadNode> Enumerate(IEnumerable<OpenScadNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Enumerate(node.Children)) yield return child;
        }
    }
}
