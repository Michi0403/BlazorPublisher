using PublisherStudio.BusinessObjects;
namespace PublisherStudio.Services;

public sealed class PublicationElementTraversal(ILogger<PublicationElementTraversal> logger)
{
    public IEnumerable<PublicationElement> Descendants(IEnumerable<PublicationElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        try
        {
            logger.LogTrace("Collecting descendant publication elements.");
            var descendants = new List<PublicationElement>();
            CollectDescendants(elements, descendants);
            logger.LogTrace("Collected {ElementCount} descendant publication elements.", descendants.Count);
            return descendants;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not collect descendant publication elements.");
            throw;
        }
    }

    public IEnumerable<PublicationElement> Descendants(PublicationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        try
        {
            logger.LogTrace("Collecting descendant publication elements for a document.");
            return Descendants(document.Pages.SelectMany(page => page.Elements));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not collect descendant publication elements for a document.");
            throw;
        }
    }

    private void CollectDescendants(
        IEnumerable<PublicationElement> elements,
        ICollection<PublicationElement> descendants)
    {
        foreach (var element in elements)
        {
            descendants.Add(element);
            if (element is not PanelElement panel) continue;
            foreach (var view in panel.Views)
                CollectDescendants(view.Elements, descendants);
        }
    }
}
