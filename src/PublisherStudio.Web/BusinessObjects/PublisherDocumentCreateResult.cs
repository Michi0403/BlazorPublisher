namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Reports the outcome of an approved document creation request back to the linked peer.
/// </summary>
public sealed class PublisherDocumentCreateResult
{
    /// <summary>Gets or sets a value indicating whether the object was created.</summary>
    /// <value>True when the object exists in the publication.</value>
    public bool Success { get; set; }

    /// <summary>Gets or sets the identifier of the created object, or an empty identifier on failure.</summary>
    /// <value>The created element identifier.</value>
    public Guid ObjectId { get; set; }

    /// <summary>Gets or sets the kind of the created object.</summary>
    /// <value>The kind name.</value>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the title of the created object.</summary>
    /// <value>The object title.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-based page number that holds the created object.</summary>
    /// <value>The one-based page number, or zero on failure.</value>
    public int Page { get; set; }

    /// <summary>Gets or sets the presentation that was actually applied.</summary>
    /// <value>The applied presentation name.</value>
    public string Presentation { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable explanation of the outcome, including what the user must do after a failure.</summary>
    /// <value>The outcome message.</value>
    public string Message { get; set; } = string.Empty;
}
