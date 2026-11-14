namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Describes one open PublisherStudio editor session (one browser circuit with its publication) as reported to a linked peer.
/// </summary>
public sealed class PublisherEditorSessionSnapshot
{
    /// <summary>Gets or sets the identifier of the editor session; pass it as sessionId to address this session.</summary>
    /// <value>The session identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the open publication.</summary>
    /// <value>The publication identifier.</value>
    public Guid PublicationId { get; set; }

    /// <summary>Gets or sets the title of the open publication.</summary>
    /// <value>The publication name.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of pages in the open publication.</summary>
    /// <value>The page count.</value>
    public int PageCount { get; set; }

    /// <summary>Gets or sets the one-based number of the page the user currently sees.</summary>
    /// <value>The current one-based page number.</value>
    public int CurrentPage { get; set; }

    /// <summary>Gets or sets the spreadsheet and text objects currently present in the publication.</summary>
    /// <value>The list of spreadsheet and text objects.</value>
    public List<PublisherEditorObjectSnapshot> Objects { get; set; } = [];
}
