using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Defines the open scad catalog service contract.
/// </summary>
public interface IOpenScadCatalogService
{
    /// <summary>
    /// Gets definitions.
    /// </summary>
    IReadOnlyList<OpenScadNodeDefinition> GetDefinitions();
    /// <summary>
    /// Runs the find operation.
    /// </summary>
    OpenScadNodeDefinition? Find(string kind);
}


/// <summary>
/// Defines the open scad node factory service contract.
/// </summary>
public interface IOpenScadNodeFactoryService
{
    /// <summary>
    /// Runs the create operation.
    /// </summary>
    OpenScadNode Create(string kind);
}

/// <summary>
/// Defines the open scad value formatter contract.
/// </summary>
public interface IOpenScadValueFormatter
{
    /// <summary>
    /// Runs the format operation.
    /// </summary>
    string Format(OpenScadValue? value, string fallbackExpression = "undef");
    /// <summary>
    /// Runs the quote operation.
    /// </summary>
    string Quote(string value);
    /// <summary>
    /// Runs the identifier operation.
    /// </summary>
    string Identifier(string value, string fallback = "part");
}

/// <summary>
/// Defines the open scad node renderer contract.
/// </summary>
public interface IOpenScadNodeRenderer
{
    /// <summary>
    /// Determines whether render.
    /// </summary>
    bool CanRender(OpenScadNode node);
    /// <summary>
    /// Runs the render operation.
    /// </summary>
    string Render(OpenScadNode node, OpenScadRenderContext context);
}

/// <summary>
/// Defines the open scad document service contract.
/// </summary>
public interface IOpenScadDocumentService
{
    /// <summary>
    /// Runs the validate operation.
    /// </summary>
    OpenScadValidationResult Validate(OpenScadDocument document);
    /// <summary>
    /// Runs the generate operation.
    /// </summary>
    OpenScadGenerationResult Generate(OpenScadDocument document);
    /// <summary>
    /// Creates example document.
    /// </summary>
    OpenScadDocument CreateExampleDocument();
}

/// <summary>
/// Defines the open scad video layer adapter contract.
/// </summary>
public interface IOpenScadVideoLayerAdapter
{
    /// <summary>
    /// Creates script.
    /// </summary>
    string CreateScript(VideoEffectLayer layer);
}

/// <summary>
/// Represents an open scad render context.
/// </summary>
public sealed class OpenScadRenderContext
{
    /// <summary>
    /// Gets or sets values.
    /// </summary>
    public required IOpenScadValueFormatter Values { get; init; }
    /// <summary>
    /// Gets or sets render node.
    /// </summary>
    public required Func<OpenScadNode, int, string> RenderNode { get; init; }
    /// <summary>
    /// Gets or sets depth.
    /// </summary>
    public int Depth { get; init; }
    /// <summary>
    /// Gets indent.
    /// </summary>
    public string Indent => new(' ', Depth * 4);
    /// <summary>
    /// Runs the child block operation.
    /// </summary>
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
