namespace PublisherStudio.Domain;

public enum InterchangeIssueSeverity { Information, Warning, Loss }

public sealed record InterchangeIssue(
    InterchangeIssueSeverity Severity,
    string Code,
    string Message,
    string? Source = null);

public sealed class PictureImportResult
{
    public PictureDocument Document { get; init; } = PictureDocument.CreateDefault();
    public List<InterchangeIssue> Issues { get; init; } = [];
}

public sealed class PublicationImportResult
{
    public PublicationDocument Document { get; init; } = PublicationDocument.CreateDefault();
    public List<InterchangeIssue> Issues { get; init; } = [];
}
