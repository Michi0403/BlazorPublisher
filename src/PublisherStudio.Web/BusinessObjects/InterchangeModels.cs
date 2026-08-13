namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported interchange issue severity values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum InterchangeIssueSeverity { Information, Warning, Loss }

/// <summary>
/// Represents an interchange issue application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Severity">Interchange issue severity dependency used by the interchange issue workflow to provide the corresponding application capability.</param>
/// <param name="Code">Code value supplied to the interchange issue operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the interchange issue operation and used when producing its result.</param>
/// <param name="Source">Source value supplied to the interchange issue operation and used when producing its result.</param>
public sealed record InterchangeIssue(
    InterchangeIssueSeverity Severity,
    string Code,
    string Message,
    string? Source = null);

/// <summary>
/// Represents the outcome of picture import, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class PictureImportResult
{
    /// <summary>
    /// Gets or sets the document value that forms part of the picture import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document value exposed by <see cref="PictureImportResult"/>.</value>
    public PictureDocument Document { get; init; } = null!;
    /// <summary>
    /// Gets or sets the issues collection maintained or exposed by this picture import instance for downstream processing.
    /// </summary>
    /// <value>The issues value exposed by <see cref="PictureImportResult"/>.</value>
    public List<InterchangeIssue> Issues { get; init; } = [];
}

/// <summary>
/// Represents the outcome of publication import, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class PublicationImportResult
{
    /// <summary>
    /// Gets or sets the document value that forms part of the publication import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document value exposed by <see cref="PublicationImportResult"/>.</value>
    public PublicationDocument Document { get; init; } = null!;
    /// <summary>
    /// Gets or sets the issues collection maintained or exposed by this publication import instance for downstream processing.
    /// </summary>
    /// <value>The issues value exposed by <see cref="PublicationImportResult"/>.</value>
    public List<InterchangeIssue> Issues { get; init; } = [];
}

/// <summary>
/// Represents the outcome of video project import, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class VideoProjectImportResult
{
    /// <summary>
    /// Gets or sets the project value that forms part of the video project import state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The project value exposed by <see cref="VideoProjectImportResult"/>.</value>
    public VideoProjectDocument Project { get; init; } = new();
    /// <summary>
    /// Gets or sets the issues collection maintained or exposed by this video project import instance for downstream processing.
    /// </summary>
    /// <value>The issues value exposed by <see cref="VideoProjectImportResult"/>.</value>
    public List<InterchangeIssue> Issues { get; init; } = [];
}
