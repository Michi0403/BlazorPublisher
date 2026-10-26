using System.Text;
using System.Text.Encodings.Web;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Defines the contract for publication markup behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublicationMarkupService
{
    /// <summary>
    /// Performs safe file name as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string SafeFileName(string value);
    /// <summary>
    /// Normalizes CSS background as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string NormalizeCssBackground(string? value);
    /// <summary>
    /// Performs sanitize preview HTML as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string SanitizePreviewHtml(string html);
    /// <summary>Describes the PublisherStudio RichEdit formatting accepted for Council proposals.</summary>
    string RichEditAiFormattingContract { get; }
    /// <summary>Converts approved model prose/Markdown to HTML with no untrusted markup.</summary>
    string FormatAiStoryForRichEdit(string text);
}

/// <summary>
/// Coordinates publication markup behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="runtimePatterns">Publisher runtime pattern service dependency used by the publication markup workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicationMarkupService(
    IPublisherRuntimePatternService runtimePatterns,
    ILogger<PublicationMarkupService> logger) : IPublicationMarkupService
{
    /// <summary>
    /// Performs safe file name as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string SafeFileName(string value)
    {
        try
        {
            logger.LogTrace($"Sanitizing a publication file name.");
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string((value ?? string.Empty).Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "publication" : safe;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not sanitize a publication file name: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes CSS background as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeCssBackground(string? value)
    {
        try
        {
            logger.LogTrace($"Normalizing a publication CSS background value.");
            if (string.IsNullOrWhiteSpace(value)) return "transparent";
            var normalized = value.Trim();
            if (normalized.Length > 512) return "transparent";
            if (normalized.IndexOfAny([';', '"', '\'', '<', '>', '{', '}']) >= 0
                || normalized.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("expression(", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("url(", StringComparison.OrdinalIgnoreCase))
            {
                return "transparent";
            }
            return normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize a publication CSS background value: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs sanitize preview HTML as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string SanitizePreviewHtml(string html)
    {
        try
        {
            logger.LogTrace($"Sanitizing publication preview HTML.");
            if (string.IsNullOrWhiteSpace(html)) return "<p></p>";
            var value = runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationDangerousElements).Replace(html, string.Empty);
            value = runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationEventAttribute).Replace(value, string.Empty);
            value = runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationJavascriptUrl).Replace(value, "$1=\"#\"");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not sanitize publication preview HTML: {exception.Message}");
            throw;
        }
    }
    /// <summary>Publisher-owned contract sent over 1-Wire with the selected Council story request.</summary>
    public string RichEditAiFormattingContract =>
        "PublisherStudio uses DevExpress RichEdit for the story. Return ONLY finished literary content " +
        "as paragraphs with optional Markdown headings, bold, italic and lists. " +
        "PublisherStudio inserts safe RichEdit HTML after user review. " +
        "Do not include a Final Answer wrapper, JSON payload, tool instructions, implementation plan, " +
        "Council receipts or internal status in the proposed story text.";

    /// <summary>Encodes untrusted Council output and applies a bounded Markdown-to-RichEdit HTML subset.</summary>
    public string FormatAiStoryForRichEdit(string text)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
            // A generic final-answer wrapper is not part of an authored story.
            var first = 0;
            while (first < lines.Length && (string.IsNullOrWhiteSpace(lines[first]) ||
                string.Equals(lines[first].Trim().Trim('*', '#', ' '), "Final Answer", StringComparison.OrdinalIgnoreCase)))
                first++;
            var html = new StringBuilder();
            var paragraph = new List<string>();
            string? listKind = null;
            void FlushParagraph()
            {
                if (paragraph.Count == 0) return;
                html.Append("<p>");
                html.Append(string.Join("<br/>", paragraph.Select(FormatInline)));
                html.Append("</p>");
                paragraph.Clear();
            }
            void EndList()
            {
                if (listKind is null) return;
                html.Append("</").Append(listKind).Append('>');
                listKind = null;
            }
            for (var index = first; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                // Do not insert a model-generated JSON tool example or Council bookkeeping into RichEdit.
                if (line.StartsWith("```json", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("## Keyaccount Manager", StringComparison.OrdinalIgnoreCase))
                    break;
                if (line.Length == 0 || line is "---" or "***")
                {
                    FlushParagraph();
                    EndList();
                    continue;
                }
                var level = 0;
                while (level < line.Length && level < 3 && line[level] == '#') level++;
                if (level > 0 && line.Length > level && line[level] == ' ')
                {
                    FlushParagraph();
                    EndList();
                    html.Append("<h").Append(level).Append('>')
                        .Append(FormatInline(line[(level + 1)..]))
                        .Append("</h").Append(level).Append('>');
                    continue;
                }
                var kind = line.StartsWith("- ", StringComparison.Ordinal) ||
                    line.StartsWith("* ", StringComparison.Ordinal) ? "ul" : null;
                var content = kind is null ? line : line[2..];
                if (kind is null)
                {
                    var separator = line.IndexOf(". ", StringComparison.Ordinal);
                    if (separator > 0 && separator <= 3 && line[..separator].All(char.IsDigit))
                    {
                        kind = "ol";
                        content = line[(separator + 2)..];
                    }
                }
                if (kind is not null)
                {
                    FlushParagraph();
                    if (listKind != kind)
                    {
                        EndList();
                        html.Append('<').Append(kind).Append('>');
                        listKind = kind;
                    }
                    html.Append("<li>").Append(FormatInline(content)).Append("</li>");
                    continue;
                }
                EndList();
                paragraph.Add(line);
            }
            FlushParagraph();
            EndList();
            return html.Length == 0 ? "<p></p>" : html.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not prepare approved Council story content for RichEdit.");
            throw;
        }
    }

    private string FormatInline(string source)
    {
        try
        {
            var safe = HtmlEncoder.Default.Encode(source);
            return ReplaceMarker(ReplaceMarker(safe, "**", "strong"), "*", "em");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not encode inline Council text for RichEdit formatting.");
            throw;
        }
    }

    private string ReplaceMarker(string text, string marker, string tag)
    {
        try
        {
            var builder = new StringBuilder();
            var position = 0;
            while (position < text.Length)
            {
                var open = text.IndexOf(marker, position, StringComparison.Ordinal);
                if (open < 0)
                {
                    builder.Append(text, position, text.Length - position);
                    break;
                }
                var close = text.IndexOf(marker, open + marker.Length, StringComparison.Ordinal);
                if (close <= open + marker.Length)
                {
                    builder.Append(text, position, text.Length - position);
                    break;
                }
                builder.Append(text, position, open - position);
                builder.Append('<').Append(tag).Append('>');
                builder.Append(text, open + marker.Length, close - open - marker.Length);
                builder.Append("</").Append(tag).Append('>');
                position = close + marker.Length;
            }
            return builder.ToString();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not apply an inline RichEdit formatting marker.");
            throw;
        }
    }

}
