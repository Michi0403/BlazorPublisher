using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

/// <summary>
/// Defines the open scad catalog service contract.
/// </summary>
public interface IOpenScadCatalogService
{
    IReadOnlyList<OpenScadNodeDefinition> GetDefinitions();
    OpenScadNodeDefinition? Find(string kind);
}


/// <summary>
/// Defines the open scad node factory service contract.
/// </summary>
public interface IOpenScadNodeFactoryService
{
    OpenScadNode Create(string kind);
}

/// <summary>
/// Defines the open scad value formatter contract.
/// </summary>
public interface IOpenScadValueFormatter
{
    string Format(OpenScadValue? value, string fallbackExpression = "undef");
    string Quote(string value);
    string Identifier(string value, string fallback = "part");
}

/// <summary>
/// Defines the open scad node renderer contract.
/// </summary>
public interface IOpenScadNodeRenderer
{
    bool CanRender(OpenScadNode node);
    string Render(OpenScadNode node, OpenScadRenderContext context);
}

/// <summary>
/// Defines the open scad document service contract.
/// </summary>
public interface IOpenScadDocumentService
{
    OpenScadValidationResult Validate(OpenScadDocument document);
    OpenScadGenerationResult Generate(OpenScadDocument document);
    OpenScadDocument CreateExampleDocument();
}

/// <summary>
/// Defines the open scad video layer adapter contract.
/// </summary>
public interface IOpenScadVideoLayerAdapter
{
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
