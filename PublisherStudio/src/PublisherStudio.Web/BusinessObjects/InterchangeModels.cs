namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported interchange issue severity values.
/// </summary>
public enum InterchangeIssueSeverity { Information, Warning, Loss }

/// <summary>
/// Represents an interchange issue.
/// </summary>
public sealed record InterchangeIssue(
    InterchangeIssueSeverity Severity,
    string Code,
    string Message,
    string? Source = null);

/// <summary>
/// Represents a picture import result.
/// </summary>
public sealed class PictureImportResult
{
    /// <summary>
    /// Gets or sets document.
    /// </summary>
    public PictureDocument Document { get; init; } = null!;
    /// <summary>
    /// Gets or sets issues.
    /// </summary>
    public List<InterchangeIssue> Issues { get; init; } = [];
}

/// <summary>
/// Represents a publication import result.
/// </summary>
public sealed class PublicationImportResult
{
    /// <summary>
    /// Gets or sets document.
    /// </summary>
    public PublicationDocument Document { get; init; } = null!;
    /// <summary>
    /// Gets or sets issues.
    /// </summary>
    public List<InterchangeIssue> Issues { get; init; } = [];
}

/// <summary>
/// Represents a video project import result.
/// </summary>
public sealed class VideoProjectImportResult
{
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public VideoProjectDocument Project { get; init; } = new();
    /// <summary>
    /// Gets or sets issues.
    /// </summary>
    public List<InterchangeIssue> Issues { get; init; } = [];
}
