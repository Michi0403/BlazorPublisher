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

    public event Action? Changed;
    public IReadOnlyList<UserNotificationMessage> Messages => _messages.AsReadOnly();

    public UserNotificationMessage Publish(UserNotificationMessage message)
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

    public UserNotificationMessage Information(string message, string title = "PublisherStudio", string source = "") =>
        Publish(Create(UserNotificationSeverity.Information, title, message, source));

    public UserNotificationMessage Success(string message, string title = "Completed", string source = "") =>
        Publish(Create(UserNotificationSeverity.Success, title, message, source));

    public UserNotificationMessage Warning(string message, string title = "Attention", string source = "") =>
        Publish(Create(UserNotificationSeverity.Warning, title, message, source));

    public UserNotificationMessage Error(string message, string title = "Something went wrong", string source = "", bool persistent = false)
    {
        var notification = Create(UserNotificationSeverity.Error, title, message, source);
        notification.Persistent = persistent;
        notification.DurationMilliseconds = persistent ? 60000 : 10000;
        return Publish(notification);
    }

    public bool Dismiss(Guid id)
    {
        var index = _messages.FindIndex(message => message.Id == id);
        if (index < 0) return false;
        _messages.RemoveAt(index);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (_messages.Count == 0) return;
        _messages.Clear();
        Changed?.Invoke();
    }

    private UserNotificationMessage Create(UserNotificationSeverity severity, string title, string message, string source) => new()
    {
        Severity = severity,
        Title = title,
        Message = message,
        Source = source
    };

    private void Log(UserNotificationMessage message)
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
}
