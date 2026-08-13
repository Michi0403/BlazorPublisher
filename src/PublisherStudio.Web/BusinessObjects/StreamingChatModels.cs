namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a platform chat message application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="OutputId">Identifier of the output to use for this operation.</param>
/// <param name="Platform">Platform value supplied to the platform chat message operation and used when producing its result.</param>
/// <param name="Channel">Channel value supplied to the platform chat message operation and used when producing its result.</param>
/// <param name="AuthorId">Identifier of the author to use for this operation.</param>
/// <param name="AuthorName">Author name value supplied to the platform chat message operation and used when producing its result.</param>
/// <param name="AuthorAvatar">Author avatar value supplied to the platform chat message operation and used when producing its result.</param>
/// <param name="Text">Text value supplied to the platform chat message operation and used when producing its result.</param>
/// <param name="Timestamp">Timestamp value supplied to the platform chat message operation and used when producing its result.</param>
/// <param name="Color">Color value supplied to the platform chat message operation and used when producing its result.</param>
/// <param name="Badges">Badges value supplied to the platform chat message operation and used when producing its result.</param>
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
/// Represents the outcome of streaming chat send, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="Exists">Value indicating whether exists should apply to this operation.</param>
/// <param name="Sent">Value indicating whether sent should apply to this operation.</param>
/// <param name="Error">Error value supplied to the streaming chat send operation and used when producing its result.</param>
public sealed record StreamingChatSendResult(bool Exists, bool Sent, string Error);
