using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Renders open OpenSCAD primitive node state into the representation required by the surrounding UI, export, or publishing workflow.
/// </summary>
/// <param name="catalog">Open openscad catalog service dependency used by the open OpenSCAD primitive node workflow to provide the corresponding application capability.</param>
public sealed class OpenScadPrimitiveNodeRenderer(IOpenScadCatalogService catalog) : IOpenScadNodeRenderer
{
    /// <summary>
    /// Stores the in-memory categories collection maintained internally by <see cref="OpenScadPrimitiveNodeRenderer"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<OpenScadNodeCategory> _categories =
    [OpenScadNodeCategory.Primitive2D, OpenScadNodeCategory.Primitive3D, OpenScadNodeCategory.Import];

    /// <summary>
    /// Determines whether render for <see cref="OpenScadPrimitiveNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD primitive node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD primitive node operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool CanRender(OpenScadNode node) {
    try
    {
        return catalog.Find(node.Kind) is { } definition && _categories.Contains(definition.Category);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadPrimitiveNodeRenderer.CanRender failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render for <see cref="OpenScadPrimitiveNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD primitive node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD primitive node operation and used when producing its result.</param>
    /// <param name="context">Context value supplied to the open OpenSCAD primitive node operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
    try
    {
            var definition = catalog.Find(node.Kind)!;
            var arguments = definition.Parameters
                .Where(parameter => node.Parameters.ContainsKey(parameter.Name) || !string.IsNullOrWhiteSpace(parameter.DefaultExpression))
                .Select(parameter => $"{parameter.Name}={context.Values.Format(node.Parameters.GetValueOrDefault(parameter.Name), parameter.DefaultExpression)}");
            return $"{context.Indent}{node.Kind}({string.Join(", ", arguments)});";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadPrimitiveNodeRenderer.Render failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Renders open OpenSCAD wrapper node state into the representation required by the surrounding UI, export, or publishing workflow.
/// </summary>
/// <param name="catalog">Open openscad catalog service dependency used by the open OpenSCAD wrapper node workflow to provide the corresponding application capability.</param>
public sealed class OpenScadWrapperNodeRenderer(IOpenScadCatalogService catalog) : IOpenScadNodeRenderer
{
    /// <summary>
    /// Stores the in-memory categories collection maintained internally by <see cref="OpenScadWrapperNodeRenderer"/> for its current workflow state.
    /// </summary>
    private readonly HashSet<OpenScadNodeCategory> _categories =
    [OpenScadNodeCategory.Transform, OpenScadNodeCategory.BooleanOperation, OpenScadNodeCategory.Extrusion, OpenScadNodeCategory.Projection, OpenScadNodeCategory.Utility];

    /// <summary>
    /// Determines whether render for <see cref="OpenScadWrapperNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD wrapper node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD wrapper node operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool CanRender(OpenScadNode node) {
    try
    {
        return catalog.Find(node.Kind) is { } definition && _categories.Contains(definition.Category);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadWrapperNodeRenderer.CanRender failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render for <see cref="OpenScadWrapperNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD wrapper node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD wrapper node operation and used when producing its result.</param>
    /// <param name="context">Context value supplied to the open OpenSCAD wrapper node operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
    try
    {
            var definition = catalog.Find(node.Kind)!;
            var arguments = definition.Parameters
                .Where(parameter => node.Parameters.ContainsKey(parameter.Name) || !string.IsNullOrWhiteSpace(parameter.DefaultExpression))
                .Select(parameter => $"{parameter.Name}={context.Values.Format(node.Parameters.GetValueOrDefault(parameter.Name), parameter.DefaultExpression)}");
            var childBlock = context.ChildBlock(node);
            return $"{context.Indent}{node.Kind}({string.Join(", ", arguments)}) {{\n{childBlock}\n{context.Indent}}}";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadWrapperNodeRenderer.Render failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Renders open OpenSCAD raw node state into the representation required by the surrounding UI, export, or publishing workflow.
/// </summary>
public sealed class OpenScadRawNodeRenderer : IOpenScadNodeRenderer
{
    /// <summary>
    /// Determines whether render for <see cref="OpenScadRawNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD raw node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD raw node operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool CanRender(OpenScadNode node) {
    try
    {
        return string.Equals(node.Kind, "raw", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadRawNodeRenderer.CanRender failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render for <see cref="OpenScadRawNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD raw node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD raw node operation and used when producing its result.</param>
    /// <param name="context">Context value supplied to the open OpenSCAD raw node operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
    try
    {
            var code = node.Parameters.GetValueOrDefault("code")?.Text ?? string.Empty;
            return string.Join(Environment.NewLine, code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => context.Indent + line));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadRawNodeRenderer.Render failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Renders open OpenSCAD module call node state into the representation required by the surrounding UI, export, or publishing workflow.
/// </summary>
/// <param name="values">Open openscad value formatter dependency used by the open OpenSCAD module call node workflow to provide the corresponding application capability.</param>
public sealed class OpenScadModuleCallNodeRenderer(IOpenScadValueFormatter values) : IOpenScadNodeRenderer
{
    /// <summary>
    /// Determines whether render for <see cref="OpenScadModuleCallNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD module call node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD module call node operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool CanRender(OpenScadNode node) {
    try
    {
        return string.Equals(node.Kind, "module_call", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadModuleCallNodeRenderer.CanRender failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs render for <see cref="OpenScadModuleCallNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD module call node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD module call node operation and used when producing its result.</param>
    /// <param name="context">Context value supplied to the open OpenSCAD module call node operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Render(OpenScadNode node, OpenScadRenderContext context)
    {
    try
    {
            var name = values.Identifier(node.Parameters.GetValueOrDefault("name")?.Text ?? node.Name, "part");
            var arguments = node.Parameters.GetValueOrDefault("arguments")?.Text?.Trim() ?? string.Empty;
            var children = context.ChildBlock(node);
            return string.IsNullOrWhiteSpace(children)
                ? $"{context.Indent}{name}({arguments});"
                : $"{context.Indent}{name}({arguments}) {{\n{children}\n{context.Indent}}}";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadModuleCallNodeRenderer.Render failed: {__serviceMethodException}");
        throw;
    }
}
}
