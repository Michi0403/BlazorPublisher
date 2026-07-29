namespace PublisherStudio.Domain;

public sealed class PublicationElementTraversal(ILogger<PublicationElementTraversal> logger)
{
    public IEnumerable<PublicationElement> Descendants(IEnumerable<PublicationElement> elements)
    {
        foreach (var element in elements)
        {
            yield return element;
            if (element is not PanelElement panel) continue;
            foreach (var child in panel.Views.SelectMany(view => Descendants(view.Elements)))
                yield return child;
        }
    }

    public IEnumerable<PublicationElement> Descendants(PublicationDocument document) =>
        document.Pages.SelectMany(page => Descendants(page.Elements));
}
