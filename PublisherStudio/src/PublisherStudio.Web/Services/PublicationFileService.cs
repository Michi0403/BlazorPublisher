using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed partial class PublicationFileService
{
    private readonly PictureDocumentService _pictures;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PublicationFileService(PictureDocumentService pictures) => _pictures = pictures;

    public string Serialize(PublicationDocument document)
    {
        document.ModifiedUtc = DateTimeOffset.UtcNow;
        return JsonSerializer.Serialize(document, _options);
    }

    public PublicationDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<PublicationDocument>(json, _options)
            ?? throw new InvalidDataException("The publication file is empty or invalid.");
        document.View ??= new PublicationViewSettings();
        document.Zoom = Math.Clamp(document.Zoom <= 0 ? .8 : document.Zoom, .2, 4);
        document.View.GridSpacingMm = Math.Clamp(document.View.GridSpacingMm <= 0 ? 5 : document.View.GridSpacingMm, .5, 100);
        document.View.ExportDpi = Math.Clamp(document.View.ExportDpi <= 0 ? 150 : document.View.ExportDpi, 72, 600);
        if (document.Pages.Count == 0)
            document.Pages.Add(PublicationPage.CreateA4());
        foreach (var text in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<TextFrameElement>())
        {
            text.PreviewHtml = SanitizePreviewHtml(text.PreviewHtml);
            if (text.DocumentContent is null || text.DocumentContent.Length == 0)
            {
                text.DocumentContent = RichTextDocumentFactory.CreateOpenXml("Text frame");
                text.StoryFormat = StoryStorageFormat.OpenXml;
            }
            else if (LooksLikeHtml(text.DocumentContent))
            {
                // Files created by v0.1/v0.2 stored stories as HTML. StoryEditor upgrades them to DOCX on first open.
                text.StoryFormat = StoryStorageFormat.Html;
            }
        }

        foreach (var image in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<ImageFrameElement>())
        {
            if (string.IsNullOrWhiteSpace(image.OriginalDataUrl)) image.OriginalDataUrl = image.DataUrl;
            image.Opacity = Math.Clamp(image.Opacity, 0, 1);
            image.TintOpacity = Math.Clamp(image.TintOpacity, 0, 1);
            image.TransparentColorTolerance = Math.Clamp(image.TransparentColorTolerance, 0, 255);
            if (image.PictureSource is not null)
                _pictures.Normalize(image.PictureSource);
        }

        foreach (var wordArt in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<WordArtElement>())
        {
            wordArt.FontSizePt = Math.Clamp(wordArt.FontSizePt, 6, 300);
            wordArt.OutlineWidth = Math.Clamp(wordArt.OutlineWidth, 0, 20);
            wordArt.ExtrudeDepth = Math.Clamp(wordArt.ExtrudeDepth, 0, 24);
            wordArt.CustomPathPoints = WordArtPathGeometry.Normalize(wordArt.CustomPathPoints);
            wordArt.PathStartOffsetPercent = Math.Clamp(wordArt.PathStartOffsetPercent, 0, 100);
            wordArt.PathBaselineOffset = Math.Clamp(wordArt.PathBaselineOffset, -80, 80);
        }

        foreach (var publicationPage in document.Pages)
        {
            var objectIds = publicationPage.Elements.Where(item => item is not ConnectorElement).Select(item => item.Id).ToHashSet();
            publicationPage.Elements.RemoveAll(item => item is ConnectorElement connector &&
                (!objectIds.Contains(connector.Source.ElementId) || !objectIds.Contains(connector.Target.ElementId) || connector.Source.ElementId == connector.Target.ElementId));
            foreach (var connector in publicationPage.Elements.OfType<ConnectorElement>())
                connector.StrokeWidthMm = Math.Clamp(connector.StrokeWidthMm <= 0 ? .7 : connector.StrokeWidthMm, .1, 12);
        }

        document.FormatVersion = "1.6";
        return document;
    }

    private static bool LooksLikeHtml(byte[] content)
    {
        var prefix = System.Text.Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, 128)).TrimStart();
        return prefix.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
            || prefix.StartsWith("<body", StringComparison.OrdinalIgnoreCase)
            || prefix.StartsWith("<p", StringComparison.OrdinalIgnoreCase)
            || prefix.StartsWith("<div", StringComparison.OrdinalIgnoreCase);
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
