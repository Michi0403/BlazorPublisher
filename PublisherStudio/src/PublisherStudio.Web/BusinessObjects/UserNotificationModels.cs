namespace PublisherStudio.BusinessObjects;

public enum UserNotificationSeverity
{
    Information,
    Success,
    Warning,
    Error
}

public sealed class UserNotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public UserNotificationSeverity Severity { get; set; } = UserNotificationSeverity.Information;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public int DurationMilliseconds { get; set; } = 7000;
    public bool Persistent { get; set; }
    public string Source { get; set; } = string.Empty;
}
