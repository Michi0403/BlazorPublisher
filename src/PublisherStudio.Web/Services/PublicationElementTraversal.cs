using PublisherStudio.BusinessObjects;
namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication element traversal application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicationElementTraversal(ILogger<PublicationElementTraversal> logger)
{
    /// <summary>
    /// Performs descendants for <see cref="PublicationElementTraversal"/>, keeping the operation consistent with the state and invariants of the surrounding publication element traversal workflow.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element traversal workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Performs descendants for <see cref="PublicationElementTraversal"/>, keeping the operation consistent with the state and invariants of the surrounding publication element traversal workflow.
    /// </summary>
    /// <param name="document">Document value supplied to the publication element traversal operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs collect descendants for <see cref="PublicationElementTraversal"/>, keeping the operation consistent with the state and invariants of the surrounding publication element traversal workflow.
    /// </summary>
    /// <param name="elements">Publication element dependency used by the publication element traversal workflow to provide the corresponding application capability.</param>
    /// <param name="descendants">Publication element dependency used by the publication element traversal workflow to provide the corresponding application capability.</param>
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
