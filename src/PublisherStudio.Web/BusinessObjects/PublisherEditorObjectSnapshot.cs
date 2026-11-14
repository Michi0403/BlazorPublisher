namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Describes one native spreadsheet or text object that is present in an open PublisherStudio publication,
/// so a linked peer can refer to it without asking a human for identifiers.
/// </summary>
public sealed class PublisherEditorObjectSnapshot
{
    /// <summary>Gets or sets the stable identifier of the publication element.</summary>
    /// <value>The element identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the object kind, either "spreadsheet" or "text".</summary>
    /// <value>The lower-case kind name used by the 1-Wire document capabilities.</value>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the object.</summary>
    /// <value>The element name shown in the editor.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-based page number that holds the object.</summary>
    /// <value>The one-based page number.</value>
    public int Page { get; set; }
}
