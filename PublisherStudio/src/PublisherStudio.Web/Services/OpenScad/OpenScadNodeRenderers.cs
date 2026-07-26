using PublisherStudio.Domain;

namespace PublisherStudio.Services.OpenScad;

public sealed class OpenScadPrimitiveNodeRenderer(IOpenScadCatalogService catalog) : IOpenScadNodeRenderer
{
    private readonly HashSet<OpenScadNodeCategory> _categories =
    [OpenScadNodeCategory.Primitive2D, OpenScadNodeCategory.Primitive3D, OpenScadNodeCategory.Import];

    public bool CanRender(OpenScadNode node) => catalog.Find(node.Kind) is { } definition && _categories.Contains(definition.Category);

    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
        var definition = catalog.Find(node.Kind)!;
        var arguments = definition.Parameters
            .Where(parameter => node.Parameters.ContainsKey(parameter.Name) || !string.IsNullOrWhiteSpace(parameter.DefaultExpression))
            .Select(parameter => $"{parameter.Name}={context.Values.Format(node.Parameters.GetValueOrDefault(parameter.Name), parameter.DefaultExpression)}");
        return $"{context.Indent}{node.Kind}({string.Join(", ", arguments)});";
    }
}

public sealed class OpenScadWrapperNodeRenderer(IOpenScadCatalogService catalog) : IOpenScadNodeRenderer
{
    private readonly HashSet<OpenScadNodeCategory> _categories =
    [OpenScadNodeCategory.Transform, OpenScadNodeCategory.BooleanOperation, OpenScadNodeCategory.Extrusion, OpenScadNodeCategory.Projection, OpenScadNodeCategory.Utility];

    public bool CanRender(OpenScadNode node) => catalog.Find(node.Kind) is { } definition && _categories.Contains(definition.Category);

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

public sealed class OpenScadRawNodeRenderer : IOpenScadNodeRenderer
{
    public bool CanRender(OpenScadNode node) => string.Equals(node.Kind, "raw", StringComparison.OrdinalIgnoreCase);

    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
        var code = node.Parameters.GetValueOrDefault("code")?.Text ?? string.Empty;
        return string.Join(Environment.NewLine, code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => context.Indent + line));
    }
}

public sealed class OpenScadModuleCallNodeRenderer(IOpenScadValueFormatter values) : IOpenScadNodeRenderer
{
    public bool CanRender(OpenScadNode node) => string.Equals(node.Kind, "module_call", StringComparison.OrdinalIgnoreCase);

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
