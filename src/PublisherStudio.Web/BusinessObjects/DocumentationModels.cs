namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Describes the generated documentation artifacts available to the running PublisherStudio build.
/// </summary>
public sealed class PublisherDocumentationStatus
{
    /// <summary>
    /// Gets or sets the version value that forms part of the publisher documentation status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the documentation status was inspected.</summary>
    /// <value>The inspected at UTC value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC time recorded by the documentation build manifest.</summary>
    /// <value>The generated at UTC value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public DateTime? GeneratedAtUtc { get; set; }

    /// <summary>Gets or sets whether the generated DocFX entry page is available.</summary>
    /// <value>The HTML available value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public bool HtmlAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether PDF available applies to the publisher documentation status state.
    /// </summary>
    /// <value>The PDF available value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public bool PdfAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether XML comments available applies to the publisher documentation status state.
    /// </summary>
    /// <value>The XML comments available value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public bool XmlCommentsAvailable { get; set; }

    /// <summary>Gets or sets the number of documented XML members in the current catalog.</summary>
    /// <value>The comment count value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public int CommentCount { get; set; }

    /// <summary>Gets or sets the application-relative URL of the generated HTML documentation.</summary>
    /// <value>The HTML URL value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public string HtmlUrl { get; set; } = "/api/documentation/html/index.html";

    /// <summary>Gets or sets the application-relative URL of the versioned PDF book.</summary>
    /// <value>The PDF URL value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public string PdfUrl { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets the application-relative URL of the searchable XML-comment catalog.</summary>
    /// <value>The comments URL value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public string CommentsUrl { get; set; } = "/api/documentation/comments";

    /// <summary>
    /// Gets or sets the PDF file name used by this publisher documentation status instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The PDF file name value exposed by <see cref="PublisherDocumentationStatus"/>.</value>
    public string PdfFileName { get; set; } = string.Empty;
}

/// <summary>
/// Represents one compiler-generated XML documentation member in a searchable form.
/// </summary>
public sealed class PublisherDocumentationComment
{
    /// <summary>
    /// Gets or sets the stable member identifier used to identify or correlate this publisher documentation comment instance with related application state.
    /// </summary>
    /// <value>The member identifier value exposed by <see cref="PublisherDocumentationComment"/>.</value>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>Gets or sets a readable name derived from the member identifier.</summary>
    /// <value>The display name value exposed by <see cref="PublisherDocumentationComment"/>.</value>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the summary value that forms part of the publisher documentation comment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="PublisherDocumentationComment"/>.</value>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remarks value that forms part of the publisher documentation comment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The remarks value exposed by <see cref="PublisherDocumentationComment"/>.</value>
    public string Remarks { get; set; } = string.Empty;
}


/// <summary>
/// Represents a publisher documentation manifest application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublisherDocumentationManifest
{
    /// <summary>Gets or sets the PublisherStudio version represented by the generated documentation.</summary>
    /// <value>The version value exposed by <see cref="PublisherDocumentationManifest"/>.</value>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the documentation was generated.</summary>
    /// <value>The generated at UTC value exposed by <see cref="PublisherDocumentationManifest"/>.</value>
    public DateTime? GeneratedAtUtc { get; set; }
}

/// <summary>Describes one safe same-origin documentation view requested by the PublisherStudio frontend.</summary>
public sealed class PublisherDocumentationViewerRequest
{
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publisher documentation viewer state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublisherDocumentationViewerRequest"/>.</value>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title value that forms part of the publisher documentation viewer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="PublisherDocumentationViewerRequest"/>.</value>
    public string Title { get; set; } = "PublisherStudio documentation";
}

/// <summary>Represents the scoped in-application documentation viewer state for one Blazor circuit.</summary>
public sealed class PublisherDocumentationViewerState
{
    /// <summary>
    /// Gets or sets a value indicating whether open applies to the publisher documentation viewer state.
    /// </summary>
    /// <value>The is open value exposed by <see cref="PublisherDocumentationViewerState"/>.</value>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publisher documentation viewer state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublisherDocumentationViewerState"/>.</value>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title value that forms part of the publisher documentation viewer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="PublisherDocumentationViewerState"/>.</value>
    public string Title { get; set; } = "PublisherStudio documentation";

    /// <summary>Gets or sets a monotonic change token used by the viewer host.</summary>
    /// <value>The revision value exposed by <see cref="PublisherDocumentationViewerState"/>.</value>
    public long Revision { get; set; }
}

/// <summary>Describes the documentation routes and availability exposed to local controllers and 1-Wire peers.</summary>
public sealed class PublisherDocumentationProfile
{
    /// <summary>
    /// Gets or sets the status value that forms part of the publisher documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="PublisherDocumentationProfile"/>.</value>
    public PublisherDocumentationStatus Status { get; set; } = new();

    /// <summary>
    /// Gets or sets the help route value that forms part of the publisher documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The help route value exposed by <see cref="PublisherDocumentationProfile"/>.</value>
    public string HelpRoute { get; set; } = "/help";

    /// <summary>
    /// Gets or sets the HTML route value that forms part of the publisher documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML route value exposed by <see cref="PublisherDocumentationProfile"/>.</value>
    public string HtmlRoute { get; set; } = "/api/documentation/html/index.html";

    /// <summary>
    /// Gets or sets the API route value that forms part of the publisher documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The API route value exposed by <see cref="PublisherDocumentationProfile"/>.</value>
    public string ApiRoute { get; set; } = "/api/documentation/html/api/index.html";

    /// <summary>
    /// Gets or sets the PDF route value that forms part of the publisher documentation profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The PDF route value exposed by <see cref="PublisherDocumentationProfile"/>.</value>
    public string PdfRoute { get; set; } = "/api/documentation/pdf";

    /// <summary>Gets or sets whether the frontend uses a focus-managed native modal viewer.</summary>
    /// <value>The supports accessible modal viewer value exposed by <see cref="PublisherDocumentationProfile"/>.</value>
    public bool SupportsAccessibleModalViewer { get; set; } = true;
}

