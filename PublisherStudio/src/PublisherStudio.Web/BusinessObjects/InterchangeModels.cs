namespace PublisherStudio.BusinessObjects;

public enum InterchangeIssueSeverity { Information, Warning, Loss }

public sealed record InterchangeIssue(
    InterchangeIssueSeverity Severity,
    string Code,
    string Message,
    string? Source = null);

public sealed class PictureImportResult
{
    public PictureDocument Document { get; init; } = null!;
    public List<InterchangeIssue> Issues { get; init; } = [];
}

public sealed class PublicationImportResult
{
    public PublicationDocument Document { get; init; } = null!;
    public List<InterchangeIssue> Issues { get; init; } = [];
}

public sealed class VideoProjectImportResult
{
    public VideoProjectDocument Project { get; init; } = new();
    public List<InterchangeIssue> Issues { get; init; } = [];
}
