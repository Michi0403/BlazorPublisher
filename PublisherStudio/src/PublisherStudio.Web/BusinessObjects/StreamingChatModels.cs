namespace PublisherStudio.BusinessObjects;

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

public sealed record StreamingChatSendResult(bool Exists, bool Sent, string Error);
