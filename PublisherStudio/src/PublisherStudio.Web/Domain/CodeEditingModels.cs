namespace PublisherStudio.Domain;

public sealed class CodeLanguageProfile
{
    public string Id { get; set; } = "text";
    public string DisplayName { get; set; } = "Plain text";
    public List<string> Extensions { get; set; } = [];
    public string LineComment { get; set; } = string.Empty;
    public string BlockCommentStart { get; set; } = string.Empty;
    public string BlockCommentEnd { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
    public string StringDelimiters { get; set; } = "\"'";
    public bool UsesBraces { get; set; }
    public bool UsesIndentation { get; set; }
}

public sealed record CodeTextRequest(string LanguageId, string Text, int IndentSize = 4);
public sealed record CodeCommentRequest(string LanguageId, string Text, bool Uncomment = false);
public sealed record CodeTextResult(string LanguageId, string Text, IReadOnlyList<CodeTokenSpan> Tokens);
public sealed record CodeTokenSpan(int Start, int Length, string Kind);
