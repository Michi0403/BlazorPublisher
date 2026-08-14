using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.UserExperience;

/// <summary>
/// Circuit-scoped notification stream used by frontend components and frontend-facing services.
/// It deliberately contains no UI dependency so the same messages can be surfaced by Blazor,
/// automation clients, diagnostics, or a future native shell.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UserNotificationService(ILogger<UserNotificationService> logger) : IUserNotificationService
{
    /// <summary>
    /// Defines the maximum messages constant used by <see cref="UserNotificationService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumMessages = 12;
    /// <summary>
    /// Stores the in-memory messages collection maintained internally by <see cref="UserNotificationService"/> for its current workflow state.
    /// </summary>
    private readonly List<UserNotificationMessage> _messages = [];
    /// <summary>
    /// Serializes notification collection access because expiry timers can dismiss messages off the renderer thread.
    /// </summary>
    private readonly object _messagesGate = new();

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="UserNotificationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Gets the messages collection maintained or exposed by this user notification instance for downstream processing.
    /// </summary>
    /// <value>The messages value exposed by <see cref="UserNotificationService"/>.</value>
    public IReadOnlyList<UserNotificationMessage> Messages
    {
        get
        {
            lock (_messagesGate) return _messages.ToArray();
        }
    }

    /// <summary>
    /// Performs publish as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    public UserNotificationMessage Publish(UserNotificationMessage message)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(message);
            message.Id = message.Id == Guid.Empty ? Guid.NewGuid() : message.Id;
            message.CreatedUtc = DateTimeOffset.UtcNow;
            message.Title = message.Title?.Trim() ?? string.Empty;
            message.Message = message.Message?.Trim() ?? string.Empty;
            message.Source = message.Source?.Trim() ?? string.Empty;
            message.DurationMilliseconds = Math.Clamp(message.DurationMilliseconds, 1500, 60000);

            lock (_messagesGate)
            {
                _messages.Insert(0, message);
                if (_messages.Count > MaximumMessages)
                    _messages.RemoveRange(MaximumMessages, _messages.Count - MaximumMessages);
            }

            Log(message);
            Changed?.Invoke();
            return message;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Publish)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Publish)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs information as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    public UserNotificationMessage Information(string message, string title = "PublisherStudio", string source = "") {
    try
    {
        return Publish(Create(UserNotificationSeverity.Information, title, message, source));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Information)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Information)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs success as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    public UserNotificationMessage Success(string message, string title = "Completed", string source = "") {
    try
    {
        return Publish(Create(UserNotificationSeverity.Success, title, message, source));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Success)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Success)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs warning as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    public UserNotificationMessage Warning(string message, string title = "Attention", string source = "") {
    try
    {
        return Publish(Create(UserNotificationSeverity.Warning, title, message, source));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Warning)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Warning)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs error as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="persistent">Value indicating whether persistent should apply to this operation.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    public UserNotificationMessage Error(string message, string title = "Something went wrong", string source = "", bool persistent = false)
    {
    try
    {
            var notification = Create(UserNotificationSeverity.Error, title, message, source);
            notification.Persistent = persistent;
            notification.DurationMilliseconds = persistent ? 60000 : 10000;
            return Publish(notification);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Error)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Error)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs dismiss as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Dismiss(Guid id)
    {
    try
    {
            bool removed;
            lock (_messagesGate)
            {
                var index = _messages.FindIndex(message => message.Id == id);
                if (index < 0) return false;
                _messages.RemoveAt(index);
                removed = true;
            }
            if (removed) Changed?.Invoke();
            return removed;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Dismiss)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Dismiss)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clear as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void Clear()
    {
    try
    {
            bool cleared;
            lock (_messagesGate)
            {
                cleared = _messages.Count > 0;
                if (cleared) _messages.Clear();
            }
            if (cleared) Changed?.Invoke();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Clear)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Clear)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs create as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="severity">Severity value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    private UserNotificationMessage Create(UserNotificationSeverity severity, string title, string message, string source) {
    try
    {
        return new()
    {
        Severity = severity,
        Title = title,
        Message = message,
        Source = source
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Create)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Create)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs log as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    private void Log(UserNotificationMessage message)
    {
    try
    {
            var source = string.IsNullOrWhiteSpace(message.Source) ? nameof(UserNotificationService) : message.Source;
            switch (message.Severity)
            {
                case UserNotificationSeverity.Error:
                    logger.LogError("User notification from {Source}: {Title} - {Message}", source, message.Title, message.Message);
                    break;
                case UserNotificationSeverity.Warning:
                    logger.LogWarning("User notification from {Source}: {Title} - {Message}", source, message.Title, message.Message);
                    break;
                case UserNotificationSeverity.Success:
                    logger.LogInformation("User success notification from {Source}: {Title} - {Message}", source, message.Title, message.Message);
                    break;
                default:
                    logger.LogInformation("User notification from {Source}: {Title} - {Message}", source, message.Title, message.Message);
                    break;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Log)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserNotificationService)}.{nameof(Log)} failed.");
        throw;
    }
}
}
