namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Carries an approved request from a linked peer to create one new native spreadsheet or text object in an open publication.
/// </summary>
public sealed class PublisherDocumentCreateRequest
{
    /// <summary>Gets or sets the kind of document to create: "spreadsheet" or "text".</summary>
    /// <value>The requested kind name.</value>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the title used as the element name and, for spreadsheets, as the file name.</summary>
    /// <value>The requested title.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the content: CSV (or a Markdown table) for a spreadsheet, plain text for a text object.</summary>
    /// <value>The delivered content.</value>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the presentation: "visible" selects the new object for the user, "silent" keeps the current page and selection.</summary>
    /// <value>The presentation name.</value>
    public string Presentation { get; set; } = "visible";

    /// <summary>Gets or sets the optional one-based target page; the current page is used when it is null.</summary>
    /// <value>The one-based page number or null.</value>
    public int? TargetPage { get; set; }
}
