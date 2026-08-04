using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OpenScad;

public interface IOpenScadCatalogService
{
    IReadOnlyList<OpenScadNodeDefinition> GetDefinitions();
    OpenScadNodeDefinition? Find(string kind);
}


public interface IOpenScadNodeFactoryService
{
    OpenScadNode Create(string kind);
}

public interface IOpenScadValueFormatter
{
    string Format(OpenScadValue? value, string fallbackExpression = "undef");
    string Quote(string value);
    string Identifier(string value, string fallback = "part");
}

public interface IOpenScadNodeRenderer
{
    bool CanRender(OpenScadNode node);
    string Render(OpenScadNode node, OpenScadRenderContext context);
}

public interface IOpenScadDocumentService
{
    OpenScadValidationResult Validate(OpenScadDocument document);
    OpenScadGenerationResult Generate(OpenScadDocument document);
    OpenScadDocument CreateExampleDocument();
}

public interface IOpenScadVideoLayerAdapter
{
    string CreateScript(VideoEffectLayer layer);
}

public sealed class OpenScadRenderContext
{
    public required IOpenScadValueFormatter Values { get; init; }
    public required Func<OpenScadNode, int, string> RenderNode { get; init; }
    public int Depth { get; init; }
    public string Indent => new(' ', Depth * 4);
    public string ChildBlock(OpenScadNode node)
    {
        var children = node.Children.Where(child => child.Enabled).Select(child => RenderNode(child, Depth + 1)).Where(code => !string.IsNullOrWhiteSpace(code));
        return string.Join(Environment.NewLine, children);
    }
}
