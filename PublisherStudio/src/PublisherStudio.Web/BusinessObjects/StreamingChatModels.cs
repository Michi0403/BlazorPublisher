namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a platform chat message.
/// </summary>
public sealed record PlatformChatMessage(
    string Id,
    Guid OutputId,
    string Platform,
    string Channel,
    string AuthorId,
    string AuthorName,
    string AuthorAvatar,
    string Text,
    DateTimeOffset Timestamp,
    string Color = "",
    string Badges = "");

/// <summary>
/// Represents a streaming chat send result.
/// </summary>
public sealed record StreamingChatSendResult(bool Exists, bool Sent, string Error);
