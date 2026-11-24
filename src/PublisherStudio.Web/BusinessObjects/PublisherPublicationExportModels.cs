namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Carries an approved request from a linked peer to export the open publication through the editor's own export path.
/// </summary>
public sealed class PublisherPublicationExportRequest
{
    /// <summary>Gets or sets what is exported: one page, one object, all pages, a single-file presentation or site, or a structured website.</summary>
    /// <value>The scope advertised by the <c>publisher.publication.export</c> catalog schema.</value>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Gets or sets the output format, validated against the render export catalog before the editor is asked.</summary>
    /// <value>The lower-case format name.</value>
    public string Format { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-based page number for a page export.</summary>
    /// <value>The page number, or <see langword="null"/> for the first page.</value>
    public int? Page { get; set; }

    /// <summary>Gets or sets the publication object identifier for an object export.</summary>
    /// <value>The element identifier text.</value>
    public string ElementId { get; set; } = string.Empty;

    /// <summary>Gets or sets the raster resolution.</summary>
    /// <value>Dots per inch; the configured default applies when the peer names none.</value>
    public int Dpi { get; set; }

    /// <summary>Gets or sets the largest file the editor may return over 1-Wire.</summary>
    /// <value>The file budget in bytes derived from <c>OrganicPlugins:MaximumMessageBytes</c>.</value>
    public int MaximumBytes { get; set; }
}

/// <summary>
/// Reports the outcome of an approved publication export back to the linked peer.
/// </summary>
public sealed class PublisherPublicationExportResult
{
    /// <summary>Gets or sets a value indicating whether the export produced a file.</summary>
    /// <value>True when <see cref="DocumentBytes"/> holds the exported file.</value>
    public bool Success { get; set; }

    /// <summary>Gets or sets the exported scope.</summary>
    /// <value>The scope name.</value>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Gets or sets the file kind, which is the lower-case extension of the returned file.</summary>
    /// <value>The file kind.</value>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIME type of the returned file.</summary>
    /// <value>The MIME type.</value>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Gets or sets the suggested file name.</summary>
    /// <value>The file name.</value>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets how many publication pages the export contains, or zero for website exports.</summary>
    /// <value>The page count.</value>
    public int PageCount { get; set; }

    /// <summary>Gets or sets the exported file returned to the approved requester.</summary>
    /// <value>The file bytes.</value>
    public byte[]? DocumentBytes { get; set; }

    /// <summary>Gets or sets a human-readable explanation of the outcome, including what the user must do after a failure.</summary>
    /// <value>The outcome message.</value>
    public string Message { get; set; } = string.Empty;
}
