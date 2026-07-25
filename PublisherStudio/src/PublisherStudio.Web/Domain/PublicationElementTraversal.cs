namespace PublisherStudio.Domain;

public static class PublicationElementTraversal
{
    public static IEnumerable<PublicationElement> Descendants(IEnumerable<PublicationElement> elements)
    {
        foreach (var element in elements)
        {
            yield return element;
            if (element is not PanelElement panel) continue;
            foreach (var child in panel.Views.SelectMany(view => Descendants(view.Elements)))
                yield return child;
        }
    }

    public static IEnumerable<PublicationElement> Descendants(PublicationDocument document) =>
        document.Pages.SelectMany(page => Descendants(page.Elements));
}
