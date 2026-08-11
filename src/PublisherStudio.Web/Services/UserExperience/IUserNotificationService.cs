using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.UserExperience;

/// <summary>
/// Defines the user notification service contract.
/// </summary>
public interface IUserNotificationService
{
    event Action? Changed;
    IReadOnlyList<UserNotificationMessage> Messages { get; }
    /// <summary>
    /// Runs the publish operation.
    /// </summary>
    UserNotificationMessage Publish(UserNotificationMessage message);
    /// <summary>
    /// Runs the information operation.
    /// </summary>
    UserNotificationMessage Information(string message, string title = "PublisherStudio", string source = "");
    /// <summary>
    /// Runs the success operation.
    /// </summary>
    UserNotificationMessage Success(string message, string title = "Completed", string source = "");
    /// <summary>
    /// Runs the warning operation.
    /// </summary>
    UserNotificationMessage Warning(string message, string title = "Attention", string source = "");
    /// <summary>
    /// Runs the error operation.
    /// </summary>
    UserNotificationMessage Error(string message, string title = "Something went wrong", string source = "", bool persistent = false);
    /// <summary>
    /// Runs the dismiss operation.
    /// </summary>
    bool Dismiss(Guid id);
    /// <summary>
    /// Runs the clear operation.
    /// </summary>
    void Clear();
}
