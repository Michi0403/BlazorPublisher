using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.UserExperience;

/// <summary>
/// Defines the contract for user notification behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="IUserNotificationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Gets the messages collection maintained or exposed by this user notification instance for downstream processing.
    /// </summary>
    /// <value>The messages value exposed by <see cref="IUserNotificationService"/>.</value>
    IReadOnlyList<UserNotificationMessage> Messages { get; }
    /// <summary>
    /// Performs publish as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    UserNotificationMessage Publish(UserNotificationMessage message);
    /// <summary>
    /// Performs information as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    UserNotificationMessage Information(string message, string title = "PublisherStudio", string source = "");
    /// <summary>
    /// Performs success as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    UserNotificationMessage Success(string message, string title = "Completed", string source = "");
    /// <summary>
    /// Performs warning as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    UserNotificationMessage Warning(string message, string title = "Attention", string source = "");
    /// <summary>
    /// Performs error as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="title">Title value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the user notification operation and used when producing its result.</param>
    /// <param name="persistent">Value indicating whether persistent should apply to this operation.</param>
    /// <returns>The user notification message produced by the operation.</returns>
    UserNotificationMessage Error(string message, string title = "Something went wrong", string source = "", bool persistent = false);
    /// <summary>
    /// Performs dismiss as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Dismiss(Guid id);
    /// <summary>
    /// Performs clear as part of the user notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    void Clear();
}
