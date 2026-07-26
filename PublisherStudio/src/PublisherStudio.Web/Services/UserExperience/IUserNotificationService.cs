using PublisherStudio.Domain;

namespace PublisherStudio.Services.UserExperience;

public interface IUserNotificationService
{
    event Action? Changed;
    IReadOnlyList<UserNotificationMessage> Messages { get; }
    UserNotificationMessage Publish(UserNotificationMessage message);
    UserNotificationMessage Information(string message, string title = "PublisherStudio", string source = "");
    UserNotificationMessage Success(string message, string title = "Completed", string source = "");
    UserNotificationMessage Warning(string message, string title = "Attention", string source = "");
    UserNotificationMessage Error(string message, string title = "Something went wrong", string source = "", bool persistent = false);
    bool Dismiss(Guid id);
    void Clear();
}
