namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a code language profile.
/// </summary>
public sealed class CodeLanguageProfile
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public string Id { get; set; } = "text";
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = "Plain text";
    /// <summary>
    /// Gets or sets extensions.
    /// </summary>
    public List<string> Extensions { get; set; } = [];
    /// <summary>
    /// Gets or sets line comment.
    /// </summary>
    public string LineComment { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets block comment start.
    /// </summary>
    public string BlockCommentStart { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets block comment end.
    /// </summary>
    public string BlockCommentEnd { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets keywords.
    /// </summary>
    public List<string> Keywords { get; set; } = [];
    /// <summary>
    /// Gets or sets string delimiters.
    /// </summary>
    public string StringDelimiters { get; set; } = "\"'";
    /// <summary>
    /// Gets or sets uses braces.
    /// </summary>
    public bool UsesBraces { get; set; }
    /// <summary>
    /// Gets or sets uses indentation.
    /// </summary>
    public bool UsesIndentation { get; set; }
}

/// <summary>
/// Represents a code text request.
/// </summary>
public sealed record CodeTextRequest(string LanguageId, string Text, int IndentSize = 4);
/// <summary>
/// Represents a code comment request.
/// </summary>
public sealed record CodeCommentRequest(string LanguageId, string Text, bool Uncomment = false);
/// <summary>
/// Represents a code text result.
/// </summary>
public sealed record CodeTextResult(string LanguageId, string Text, IReadOnlyList<CodeTokenSpan> Tokens);
/// <summary>
/// Represents a code token span.
/// </summary>
public sealed record CodeTokenSpan(int Start, int Length, string Kind);
