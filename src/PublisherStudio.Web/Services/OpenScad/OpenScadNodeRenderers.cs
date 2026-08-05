using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Provides open scad primitive node renderer operations.
/// </summary>
public sealed class OpenScadPrimitiveNodeRenderer(IOpenScadCatalogService catalog) : IOpenScadNodeRenderer
{
    private readonly HashSet<OpenScadNodeCategory> _categories =
    [OpenScadNodeCategory.Primitive2D, OpenScadNodeCategory.Primitive3D, OpenScadNodeCategory.Import];

    /// <summary>
    /// Determines whether render.
    /// </summary>
    public bool CanRender(OpenScadNode node) => catalog.Find(node.Kind) is { } definition && _categories.Contains(definition.Category);

    /// <summary>
    /// Runs the render operation.
    /// </summary>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
        var definition = catalog.Find(node.Kind)!;
        var arguments = definition.Parameters
            .Where(parameter => node.Parameters.ContainsKey(parameter.Name) || !string.IsNullOrWhiteSpace(parameter.DefaultExpression))
            .Select(parameter => $"{parameter.Name}={context.Values.Format(node.Parameters.GetValueOrDefault(parameter.Name), parameter.DefaultExpression)}");
        return $"{context.Indent}{node.Kind}({string.Join(", ", arguments)});";
    }
}

/// <summary>
/// Provides open scad wrapper node renderer operations.
/// </summary>
public sealed class OpenScadWrapperNodeRenderer(IOpenScadCatalogService catalog) : IOpenScadNodeRenderer
{
    private readonly HashSet<OpenScadNodeCategory> _categories =
    [OpenScadNodeCategory.Transform, OpenScadNodeCategory.BooleanOperation, OpenScadNodeCategory.Extrusion, OpenScadNodeCategory.Projection, OpenScadNodeCategory.Utility];

    /// <summary>
    /// Determines whether render.
    /// </summary>
    public bool CanRender(OpenScadNode node) => catalog.Find(node.Kind) is { } definition && _categories.Contains(definition.Category);

    /// <summary>
    /// Runs the render operation.
    /// </summary>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
        var definition = catalog.Find(node.Kind)!;
        var arguments = definition.Parameters
            .Where(parameter => node.Parameters.ContainsKey(parameter.Name) || !string.IsNullOrWhiteSpace(parameter.DefaultExpression))
            .Select(parameter => $"{parameter.Name}={context.Values.Format(node.Parameters.GetValueOrDefault(parameter.Name), parameter.DefaultExpression)}");
        var childBlock = context.ChildBlock(node);
        return $"{context.Indent}{node.Kind}({string.Join(", ", arguments)}) {{\n{childBlock}\n{context.Indent}}}";
    }
}

/// <summary>
/// Provides open scad raw node renderer operations.
/// </summary>
public sealed class OpenScadRawNodeRenderer : IOpenScadNodeRenderer
{
    /// <summary>
    /// Determines whether render.
    /// </summary>
    public bool CanRender(OpenScadNode node) => string.Equals(node.Kind, "raw", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the render operation.
    /// </summary>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
        var code = node.Parameters.GetValueOrDefault("code")?.Text ?? string.Empty;
        return string.Join(Environment.NewLine, code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => context.Indent + line));
    }
}

/// <summary>
/// Provides open scad module call node renderer operations.
/// </summary>
public sealed class OpenScadModuleCallNodeRenderer(IOpenScadValueFormatter values) : IOpenScadNodeRenderer
{
    /// <summary>
    /// Determines whether render.
    /// </summary>
    public bool CanRender(OpenScadNode node) => string.Equals(node.Kind, "module_call", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the render operation.
    /// </summary>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
        var name = values.Identifier(node.Parameters.GetValueOrDefault("name")?.Text ?? node.Name, "part");
        var arguments = node.Parameters.GetValueOrDefault("arguments")?.Text?.Trim() ?? string.Empty;
        var children = context.ChildBlock(node);
        return string.IsNullOrWhiteSpace(children)
            ? $"{context.Indent}{name}({arguments});"
            : $"{context.Indent}{name}({arguments}) {{\n{children}\n{context.Indent}}}";
    }
}
