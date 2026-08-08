using PublisherStudio.BusinessObjects;
namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication element traversal.
/// </summary>
public sealed class PublicationElementTraversal(ILogger<PublicationElementTraversal> logger)
{
    /// <summary>
    /// Runs the descendants operation.
    /// </summary>
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

    /// <summary>
    /// Runs the descendants operation.
    /// </summary>
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
    try
    {
            foreach (var element in elements)
            {
                descendants.Add(element);
                if (element is not PanelElement panel) continue;
                foreach (var view in panel.Views)
                    CollectDescendants(view.Elements, descendants);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationElementTraversal)}.{nameof(CollectDescendants)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationElementTraversal)}.{nameof(CollectDescendants)} failed.");
        throw;
    }
}
}
