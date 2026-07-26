using System.Text;
using System.Text.RegularExpressions;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.CodeEditing;

public sealed partial class CodeFormattingService(ICodeLanguageService languages) : ICodeFormattingService
{
    public CodeTextResult Format(CodeTextRequest request)
    {
        var profile = languages.Get(request.LanguageId);
        var indentSize = Math.Clamp(request.IndentSize, 1, 8);
        var source = NormalizeLines(request.Text);
        var lines = source.Split('\n');
        var output = new StringBuilder(source.Length + lines.Length * indentSize);
        var depth = 0;
        foreach (var original in lines)
        {
            var line = original.Trim();
            if (profile.UsesBraces && StartsWithClosingToken(line)) depth = Math.Max(0, depth - 1);
            output.Append(' ', depth * indentSize).Append(line).Append('\n');
            if (profile.UsesBraces) depth = Math.Max(0, depth + BraceDelta(line));
            else if (profile.UsesIndentation && line.EndsWith(':')) depth++;
            if (profile.UsesIndentation && string.IsNullOrWhiteSpace(line)) depth = Math.Max(0, depth - 1);
        }
        var formatted = output.ToString().TrimEnd('\n');
        return new CodeTextResult(profile.Id, formatted, Tokenize(profile, formatted));
    }

    public CodeTextResult ToggleComment(CodeCommentRequest request)
    {
        var profile = languages.Get(request.LanguageId);
        var source = NormalizeLines(request.Text);
        string result;
        if (!string.IsNullOrWhiteSpace(profile.LineComment))
        {
            var lines = source.Split('\n');
            var shouldUncomment = request.Uncomment || lines.Where(line => !string.IsNullOrWhiteSpace(line)).All(line => line.TrimStart().StartsWith(profile.LineComment, StringComparison.Ordinal));
            result = string.Join("\n", lines.Select(line => shouldUncomment ? RemoveLineComment(line, profile.LineComment) : AddLineComment(line, profile.LineComment)));
        }
        else if (!string.IsNullOrWhiteSpace(profile.BlockCommentStart))
        {
            var trimmed = source.Trim();
            var isCommented = trimmed.StartsWith(profile.BlockCommentStart, StringComparison.Ordinal) && trimmed.EndsWith(profile.BlockCommentEnd, StringComparison.Ordinal);
            result = request.Uncomment || isCommented
                ? trimmed[profile.BlockCommentStart.Length..^profile.BlockCommentEnd.Length].Trim()
                : $"{profile.BlockCommentStart}\n{source}\n{profile.BlockCommentEnd}";
        }
        else result = source;
        return new CodeTextResult(profile.Id, result, Tokenize(profile, result));
    }

    public CodeTextResult Analyze(CodeTextRequest request)
    {
        var profile = languages.Get(request.LanguageId);
        var text = NormalizeLines(request.Text);
        return new CodeTextResult(profile.Id, text, Tokenize(profile, text));
    }

    private IReadOnlyList<CodeTokenSpan> Tokenize(CodeLanguageProfile profile, string text)
    {
        var spans = new List<CodeTokenSpan>();
        foreach (Match match in StringPattern().Matches(text)) spans.Add(new(match.Index, match.Length, "string"));
        foreach (Match match in NumberPattern().Matches(text)) spans.Add(new(match.Index, match.Length, "number"));
        if (!string.IsNullOrWhiteSpace(profile.LineComment))
            foreach (Match match in Regex.Matches(text, $"{Regex.Escape(profile.LineComment)}.*$", RegexOptions.Multiline)) spans.Add(new(match.Index, match.Length, "comment"));
        if (!string.IsNullOrWhiteSpace(profile.BlockCommentStart) && !string.IsNullOrWhiteSpace(profile.BlockCommentEnd))
            foreach (Match match in Regex.Matches(text, $"{Regex.Escape(profile.BlockCommentStart)}[\\s\\S]*?{Regex.Escape(profile.BlockCommentEnd)}")) spans.Add(new(match.Index, match.Length, "comment"));
        if (profile.Keywords.Count > 0)
        {
            var pattern = $"\\b(?:{string.Join('|', profile.Keywords.Select(Regex.Escape))})\\b";
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase)) spans.Add(new(match.Index, match.Length, "keyword"));
        }
        return spans.OrderBy(span => span.Start).ThenByDescending(span => span.Length).ToList().AsReadOnly();
    }

    private string NormalizeLines(string? text) => (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    private bool StartsWithClosingToken(string line) => line.StartsWith('}') || line.StartsWith(']') || line.StartsWith(')');
    private int BraceDelta(string line) => line.Count(character => character is '{' or '[') - line.Count(character => character is '}' or ']');
    private string AddLineComment(string line, string token) => string.IsNullOrWhiteSpace(line) ? line : $"{new string(' ', line.Length - line.TrimStart().Length)}{token} {line.TrimStart()}";
    private string RemoveLineComment(string line, string token)
    {
        var indent = line.Length - line.TrimStart().Length;
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith(token, StringComparison.Ordinal)) return line;
        trimmed = trimmed[token.Length..].TrimStart();
        return new string(' ', indent) + trimmed;
    }

    [GeneratedRegex("(?:\\\"(?:\\\\.|[^\\\"])*\\\"|'(?:\\\\.|[^'])*')", RegexOptions.Compiled)]
    private static partial Regex StringPattern();
    [GeneratedRegex("\\b(?:0x[0-9a-f]+|\\d+(?:\\.\\d+)?)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NumberPattern();
}
