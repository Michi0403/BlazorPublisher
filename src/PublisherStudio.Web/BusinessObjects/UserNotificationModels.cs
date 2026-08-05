namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported user notification severity values.
/// </summary>
public enum UserNotificationSeverity
{
    Information,
    Success,
    Warning,
    Error
}

/// <summary>
/// Represents an user notification message.
/// </summary>
public sealed class UserNotificationMessage
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets severity.
    /// </summary>
    public UserNotificationSeverity Severity { get; set; } = UserNotificationSeverity.Information;
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the UTC creation time.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets duration milliseconds.
    /// </summary>
    public int DurationMilliseconds { get; set; } = 7000;
    /// <summary>
    /// Gets or sets persistent.
    /// </summary>
    public bool Persistent { get; set; }
    /// <summary>
    /// Gets or sets source.
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
