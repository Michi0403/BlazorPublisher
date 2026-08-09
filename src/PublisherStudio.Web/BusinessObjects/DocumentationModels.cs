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
    public string HtmlUrl { get; set; } = "/api/documentation/html/index.html";

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

/// <summary>Describes one safe same-origin documentation view requested by the PublisherStudio frontend.</summary>
public sealed class PublisherDocumentationViewerRequest
{
    /// <summary>Gets or sets the application-relative documentation URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the accessible dialog and iframe title.</summary>
    public string Title { get; set; } = "PublisherStudio documentation";
}

/// <summary>Represents the scoped in-application documentation viewer state for one Blazor circuit.</summary>
public sealed class PublisherDocumentationViewerState
{
    /// <summary>Gets or sets whether the native modal dialog is open.</summary>
    public bool IsOpen { get; set; }

    /// <summary>Gets or sets the application-relative documentation URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the accessible dialog and iframe title.</summary>
    public string Title { get; set; } = "PublisherStudio documentation";

    /// <summary>Gets or sets a monotonic change token used by the viewer host.</summary>
    public long Revision { get; set; }
}

/// <summary>Describes the documentation routes and availability exposed to local controllers and 1-Wire peers.</summary>
public sealed class PublisherDocumentationProfile
{
    /// <summary>Gets or sets the current generated-documentation status.</summary>
    public PublisherDocumentationStatus Status { get; set; } = new();

    /// <summary>Gets or sets the in-application help route.</summary>
    public string HelpRoute { get; set; } = "/help";

    /// <summary>Gets or sets the HTML documentation route.</summary>
    public string HtmlRoute { get; set; } = "/api/documentation/html/index.html";

    /// <summary>Gets or sets the API reference route.</summary>
    public string ApiRoute { get; set; } = "/api/documentation/html/api/index.html";

    /// <summary>Gets or sets the inline PDF controller route.</summary>
    public string PdfRoute { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets whether the frontend uses a focus-managed native modal viewer.</summary>
    public bool SupportsAccessibleModalViewer { get; set; } = true;
}

