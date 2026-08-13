using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Defines the contract for open OpenSCAD catalog behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOpenScadCatalogService
{
    /// <summary>
    /// Retrieves definitions as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OpenScadNodeDefinition> GetDefinitions();
    /// <summary>
    /// Performs find as part of the open OpenSCAD catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD catalog operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node definition produced by the operation.</returns>
    OpenScadNodeDefinition? Find(string kind);
}


/// <summary>
/// Defines the contract for open OpenSCAD node factory behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOpenScadNodeFactoryService
{
    /// <summary>
    /// Performs create as part of the open OpenSCAD node factory service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="kind">Kind value supplied to the open OpenSCAD node factory operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD node produced by the operation.</returns>
    OpenScadNode Create(string kind);
}

/// <summary>
/// Defines the contract for open OpenSCAD value formatter behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOpenScadValueFormatter
{
    /// <summary>
    /// Performs format for <see cref="IOpenScadValueFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD value formatter workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the open OpenSCAD value formatter operation and used when producing its result.</param>
    /// <param name="fallbackExpression">Fallback expression value supplied to the open OpenSCAD value formatter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string Format(OpenScadValue? value, string fallbackExpression = "undef");
    /// <summary>
    /// Performs quote for <see cref="IOpenScadValueFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD value formatter workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the open OpenSCAD value formatter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string Quote(string value);
    /// <summary>
    /// Performs identifier for <see cref="IOpenScadValueFormatter"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD value formatter workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the open OpenSCAD value formatter operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the open OpenSCAD value formatter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string Identifier(string value, string fallback = "part");
}

/// <summary>
/// Defines the contract for open OpenSCAD node behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOpenScadNodeRenderer
{
    /// <summary>
    /// Determines whether render for <see cref="IOpenScadNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD node operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool CanRender(OpenScadNode node);
    /// <summary>
    /// Performs render for <see cref="IOpenScadNodeRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD node workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD node operation and used when producing its result.</param>
    /// <param name="context">Context value supplied to the open OpenSCAD node operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string Render(OpenScadNode node, OpenScadRenderContext context);
}

/// <summary>
/// Defines the contract for open OpenSCAD document behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOpenScadDocumentService
{
    /// <summary>
    /// Performs validate as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD validation result produced by the operation.</returns>
    OpenScadValidationResult Validate(OpenScadDocument document);
    /// <summary>
    /// Performs generate as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the open OpenSCAD document operation and used when producing its result.</param>
    /// <returns>The open OpenSCAD generation result produced by the operation.</returns>
    OpenScadGenerationResult Generate(OpenScadDocument document);
    /// <summary>
    /// Creates example document as part of the open OpenSCAD document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The open OpenSCAD document produced by the operation.</returns>
    OpenScadDocument CreateExampleDocument();
}

/// <summary>
/// Defines the contract for open OpenSCAD video layer adapter behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOpenScadVideoLayerAdapter
{
    /// <summary>
    /// Creates script for <see cref="IOpenScadVideoLayerAdapter"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD video layer adapter workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the open OpenSCAD video layer adapter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string CreateScript(VideoEffectLayer layer);
}

/// <summary>
/// Represents an open OpenSCAD render context application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OpenScadRenderContext
{
    /// <summary>
    /// Gets or sets the values value that forms part of the open OpenSCAD render context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The values value exposed by <see cref="OpenScadRenderContext"/>.</value>
    public required IOpenScadValueFormatter Values { get; init; }
    /// <summary>
    /// Gets or sets the render node value that forms part of the open OpenSCAD render context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The render node value exposed by <see cref="OpenScadRenderContext"/>.</value>
    public required Func<OpenScadNode, int, string> RenderNode { get; init; }
    /// <summary>
    /// Gets or sets the depth value that forms part of the open OpenSCAD render context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The depth value exposed by <see cref="OpenScadRenderContext"/>.</value>
    public int Depth { get; init; }
    /// <summary>
    /// Gets the indent value that forms part of the open OpenSCAD render context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The indent value exposed by <see cref="OpenScadRenderContext"/>.</value>
    public string Indent => new(' ', Depth * 4);
    /// <summary>
    /// Performs child block for <see cref="OpenScadRenderContext"/>, keeping the operation consistent with the state and invariants of the surrounding open OpenSCAD render context workflow.
    /// </summary>
    /// <param name="node">Node value supplied to the open OpenSCAD render context operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ChildBlock(OpenScadNode node)
    {
    try
    {
            var children = node.Children.Where(child => child.Enabled).Select(child => RenderNode(child, Depth + 1)).Where(code => !string.IsNullOrWhiteSpace(code));
            return string.Join(Environment.NewLine, children);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OpenScadRenderContext.ChildBlock failed: {__serviceMethodException}");
        throw;
    }
}
}
