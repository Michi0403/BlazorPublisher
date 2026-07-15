using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed partial class PublicationFileService
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize(PublicationDocument document)
    {
        document.ModifiedUtc = DateTimeOffset.UtcNow;
        return JsonSerializer.Serialize(document, _options);
    }

    public PublicationDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<PublicationDocument>(json, _options)
            ?? throw new InvalidDataException("The publication file is empty or invalid.");
        if (document.Pages.Count == 0)
            document.Pages.Add(PublicationPage.CreateA4());
        foreach (var text in document.Pages.SelectMany(p => p.Elements).OfType<TextFrameElement>())
            text.PreviewHtml = SanitizePreviewHtml(text.PreviewHtml);
        return document;
    }

    public static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "publication" : safe;
    }

    public static string ExtractHtmlBody(byte[] htmlBytes)
    {
        var html = System.Text.Encoding.UTF8.GetString(htmlBytes);
        var match = BodyRegex().Match(html);
        return SanitizePreviewHtml(match.Success ? match.Groups[1].Value : html);
    }

    public static string SanitizePreviewHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "<p></p>";
        var value = DangerousElementsRegex().Replace(html, string.Empty);
        value = EventAttributeRegex().Replace(value, string.Empty);
        value = JavascriptUrlRegex().Replace(value, "$1=\"#\"");
        return value;
    }

    [GeneratedRegex(@"<body[^>]*>([\s\S]*?)</body>", RegexOptions.IgnoreCase)]
    private static partial Regex BodyRegex();
    [GeneratedRegex(@"<(script|iframe|object|embed|form|input|button|meta|link)[^>]*>[\s\S]*?</\1\s*>|<(script|iframe|object|embed|form|input|button|meta|link)[^>]*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousElementsRegex();
    [GeneratedRegex("\\s+on[a-z]+\\s*=\\s*(?:\\\"[^\\\"]*\\\"|'[^']*'|[^\\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventAttributeRegex();
    [GeneratedRegex("(href|src)\\s*=\\s*[\\\"']?\\s*javascript:[^\\s>\\\"']*", RegexOptions.IgnoreCase)]
    private static partial Regex JavascriptUrlRegex();
}
