namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Describes the generated documentation artifacts available to the running PublisherStudio build.
/// </summary>
public sealed class PublisherDocumentationStatus
{
    /// <summary>Gets or sets the application version represented by the documentation.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the documentation status was inspected.</summary>
    public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC time recorded by the documentation build manifest.</summary>
    public DateTime? GeneratedAtUtc { get; set; }

    /// <summary>Gets or sets whether the generated DocFX entry page is available.</summary>
    public bool HtmlAvailable { get; set; }

    /// <summary>Gets or sets whether the versioned PDF book is available.</summary>
    public bool PdfAvailable { get; set; }

    /// <summary>Gets or sets whether compiler-generated XML documentation is available.</summary>
    public bool XmlCommentsAvailable { get; set; }

    /// <summary>Gets or sets the number of documented XML members in the current catalog.</summary>
    public int CommentCount { get; set; }

    /// <summary>Gets or sets the application-relative URL of the generated HTML documentation.</summary>
    public string HtmlUrl { get; set; } = "/help-docs/index.html";

    /// <summary>Gets or sets the application-relative URL of the versioned PDF book.</summary>
    public string PdfUrl { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets the application-relative URL of the searchable XML-comment catalog.</summary>
    public string CommentsUrl { get; set; } = "/api/documentation/comments";

    /// <summary>Gets or sets the generated PDF file name.</summary>
    public string PdfFileName { get; set; } = string.Empty;
}

/// <summary>
/// Represents one compiler-generated XML documentation member in a searchable form.
/// </summary>
public sealed class PublisherDocumentationComment
{
    /// <summary>Gets or sets the stable compiler XML member identifier.</summary>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>Gets or sets a readable name derived from the member identifier.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the concise maintained summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets optional extended guidance.</summary>
    public string Remarks { get; set; } = string.Empty;
}


/// <summary>
/// Represents the build manifest written beside the generated documentation.
/// </summary>
public sealed class PublisherDocumentationManifest
{
    /// <summary>Gets or sets the PublisherStudio version represented by the generated documentation.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the documentation was generated.</summary>
    public DateTime? GeneratedAtUtc { get; set; }
}
