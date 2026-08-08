using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.UserExperience;

/// <summary>
/// Circuit-scoped notification stream used by frontend components and frontend-facing services.
/// It deliberately contains no UI dependency so the same messages can be surfaced by Blazor,
/// automation clients, diagnostics, or a future native shell.
/// </summary>
public sealed class UserNotificationService(ILogger<UserNotificationService> logger) : IUserNotificationService
{
    private const int MaximumMessages = 12;
    private readonly List<UserNotificationMessage> _messages = [];

    /// <summary>
    /// Occurs when changed.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Gets messages.
    /// </summary>
    public IReadOnlyList<UserNotificationMessage> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Runs the publish operation.
    /// </summary>
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

            _messages.Insert(0, message);
            if (_messages.Count > MaximumMessages)
                _messages.RemoveRange(MaximumMessages, _messages.Count - MaximumMessages);

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
    /// Runs the information operation.
    /// </summary>
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
    /// Runs the success operation.
    /// </summary>
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
    /// Runs the warning operation.
    /// </summary>
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
    /// Runs the error operation.
    /// </summary>
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
    /// Runs the dismiss operation.
    /// </summary>
    public bool Dismiss(Guid id)
    {
    try
    {
            var index = _messages.FindIndex(message => message.Id == id);
            if (index < 0) return false;
            _messages.RemoveAt(index);
            Changed?.Invoke();
            return true;
    
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
    /// Runs the clear operation.
    /// </summary>
    public void Clear()
    {
    try
    {
            if (_messages.Count == 0) return;
            _messages.Clear();
            Changed?.Invoke();
    
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
