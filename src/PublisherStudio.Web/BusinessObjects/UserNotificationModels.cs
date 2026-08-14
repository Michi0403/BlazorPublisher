namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported user notification severity values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum UserNotificationSeverity
{
    Information,
    Success,
    Warning,
    Error
}

/// <summary>
/// Represents an user notification message application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class UserNotificationMessage
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this user notification message instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="UserNotificationMessage"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the severity value that forms part of the user notification message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The severity value exposed by <see cref="UserNotificationMessage"/>.</value>
    public UserNotificationSeverity Severity { get; set; } = UserNotificationSeverity.Information;
    /// <summary>
    /// Gets or sets the title value that forms part of the user notification message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="UserNotificationMessage"/>.</value>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the message value that forms part of the user notification message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="UserNotificationMessage"/>.</value>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created UTC associated with this user notification message state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="UserNotificationMessage"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the duration milliseconds value that forms part of the user notification message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The duration milliseconds value exposed by <see cref="UserNotificationMessage"/>.</value>
    public int DurationMilliseconds { get; set; } = 10000;
    /// <summary>
    /// Gets or sets a value indicating whether persistent applies to the user notification message state.
    /// </summary>
    /// <value>The persistent value exposed by <see cref="UserNotificationMessage"/>.</value>
    public bool Persistent { get; set; }
    /// <summary>
    /// Gets or sets the source value that forms part of the user notification message state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="UserNotificationMessage"/>.</value>
    public string Source { get; set; } = string.Empty;
}
