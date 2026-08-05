using System.Text;
using System.Text.RegularExpressions;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services.CodeEditing;

/// <summary>
/// Provides code formatting service operations.
/// </summary>
public sealed class CodeFormattingService(
    ICodeLanguageService languages,
    IPublisherRuntimePatternService runtimePatterns,
    ILogger<CodeFormattingService> logger) : ICodeFormattingService
{
    /// <summary>
    /// Runs the format operation.
    /// </summary>
    public CodeTextResult Format(CodeTextRequest request)
    {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.Format.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.Format failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the toggle comment operation.
    /// </summary>
    public CodeTextResult ToggleComment(CodeCommentRequest request)
    {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.ToggleComment.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.ToggleComment failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the analyze operation.
    /// </summary>
    public CodeTextResult Analyze(CodeTextRequest request)
    {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.Analyze.");
                    var profile = languages.Get(request.LanguageId);
                    var text = NormalizeLines(request.Text);
                    return new CodeTextResult(profile.Id, text, Tokenize(profile, text));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.Analyze failed: {exception.Message}");
            throw;
        }
    }

    private IReadOnlyList<CodeTokenSpan> Tokenize(CodeLanguageProfile profile, string text)
    {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.Tokenize.");
                    var spans = new List<CodeTokenSpan>();
                    foreach (Match match in runtimePatterns.GetRegex(PublisherRuntimePattern.CodeString).Matches(text)) spans.Add(new(match.Index, match.Length, "string"));
                    foreach (Match match in runtimePatterns.GetRegex(PublisherRuntimePattern.CodeNumber).Matches(text)) spans.Add(new(match.Index, match.Length, "number"));
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.Tokenize failed: {exception.Message}");
            throw;
        }
    }

    private string NormalizeLines(string? text) {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.NormalizeLines.");
            return (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.NormalizeLines failed: {exception.Message}");
            throw;
        }
    }
    private bool StartsWithClosingToken(string line) {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.StartsWithClosingToken.");
            return line.StartsWith('}') || line.StartsWith(']') || line.StartsWith(')');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.StartsWithClosingToken failed: {exception.Message}");
            throw;
        }
    }
    private int BraceDelta(string line) {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.BraceDelta.");
            return line.Count(character => character is '{' or '[') - line.Count(character => character is '}' or ']');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.BraceDelta failed: {exception.Message}");
            throw;
        }
    }
    private string AddLineComment(string line, string token) {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.AddLineComment.");
            return string.IsNullOrWhiteSpace(line) ? line : $"{new string(' ', line.Length - line.TrimStart().Length)}{token} {line.TrimStart()}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.AddLineComment failed: {exception.Message}");
            throw;
        }
    }
    private string RemoveLineComment(string line, string token)
    {
        try
        {
            logger.LogTrace($"Entering CodeFormattingService.RemoveLineComment.");
                    var indent = line.Length - line.TrimStart().Length;
                    var trimmed = line.TrimStart();
                    if (!trimmed.StartsWith(token, StringComparison.Ordinal)) return line;
                    trimmed = trimmed[token.Length..].TrimStart();
                    return new string(' ', indent) + trimmed;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"CodeFormattingService.RemoveLineComment failed: {exception.Message}");
            throw;
        }
    }

}
