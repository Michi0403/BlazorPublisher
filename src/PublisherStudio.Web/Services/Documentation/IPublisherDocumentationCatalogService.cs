using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Documentation;

/// <summary>
/// Resolves generated PublisherStudio documentation without exposing arbitrary filesystem paths.
/// </summary>
public interface IPublisherDocumentationCatalogService
{
    /// <summary>Returns availability information for the generated HTML, PDF, and XML documentation.</summary>
    PublisherDocumentationStatus GetStatus();

    /// <summary>Resolves a safe generated documentation file path for an application-relative request.</summary>
    /// <param name="relativePath">The relative documentation path, or <see langword="null"/> for the index page.</param>
    /// <returns>The full file path when it exists below the documentation root; otherwise <see langword="null"/>.</returns>
    string? GetHtmlFilePath(string? relativePath);

    /// <summary>Resolves the generated PDF book for the current application version.</summary>
    /// <returns>The PDF path when available; otherwise <see langword="null"/>.</returns>
    string? GetPdfPath();

    /// <summary>Searches compiler-generated XML comments by identifier, display name, summary, or remarks.</summary>
    /// <param name="query">Optional case-insensitive search text.</param>
    /// <param name="limit">Maximum number of comments to return.</param>
    IReadOnlyList<PublisherDocumentationComment> SearchComments(string? query, int limit);
}
