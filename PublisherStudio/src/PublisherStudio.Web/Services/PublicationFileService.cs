using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed partial class PublicationFileService
{
    private readonly PictureDocumentService _pictures;
    private readonly PublicationDataService _data;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PublicationFileService(PictureDocumentService pictures, PublicationDataService data)
    {
        _pictures = pictures;
        _data = data;
    }

    public string Serialize(PublicationDocument document)
    {
        document.ModifiedUtc = DateTimeOffset.UtcNow;
        return JsonSerializer.Serialize(document, _options);
    }

    public PublicationElement CloneElement(PublicationElement element) =>
        JsonSerializer.Deserialize<PublicationElement>(JsonSerializer.Serialize<PublicationElement>(element, _options), _options)
        ?? throw new InvalidDataException("The publication element could not be cloned.");

    public PublicationPage ClonePage(PublicationPage publicationPage) =>
        JsonSerializer.Deserialize<PublicationPage>(JsonSerializer.Serialize(publicationPage, _options), _options)
        ?? throw new InvalidDataException("The publication page could not be cloned.");

    public PublicationDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<PublicationDocument>(json, _options)
            ?? throw new InvalidDataException("The publication file is empty or invalid.");
        document.View ??= new PublicationViewSettings();
        document.Playback ??= new PublicationPlaybackSettings();
        _data.Normalize(document);
        document.Zoom = Math.Clamp(document.Zoom <= 0 ? .8 : document.Zoom, .2, 4);
        document.View.GridSpacingMm = Math.Clamp(document.View.GridSpacingMm <= 0 ? 5 : document.View.GridSpacingMm, .5, 100);
        document.View.ExportDpi = Math.Clamp(document.View.ExportDpi <= 0 ? 150 : document.View.ExportDpi, 72, 600);
        if (document.Pages.Count == 0)
            document.Pages.Add(PublicationPage.CreateA4());
        foreach (var text in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<TextFrameElement>())
        {
            text.PreviewHtml = SanitizePreviewHtml(text.PreviewHtml);
            text.PaddingMm = Math.Clamp(text.PaddingMm, 0, 50);
            text.BorderWidth = Math.Clamp(text.BorderWidth, 0, 5);
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

        foreach (var media in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<PublicationMediaElement>())
        {
            media.DurationSeconds = Math.Clamp(media.DurationSeconds, 0, 24 * 60 * 60);
            media.TrimStartSeconds = Math.Clamp(media.TrimStartSeconds, 0, Math.Max(0, media.DurationSeconds));
            var trimEnd = media.TrimEndSeconds <= media.TrimStartSeconds ? media.DurationSeconds : media.TrimEndSeconds;
            media.TrimEndSeconds = Math.Clamp(trimEnd, media.TrimStartSeconds, Math.Max(media.TrimStartSeconds, media.DurationSeconds));
            media.TimelineStartSeconds = Math.Clamp(media.TimelineStartSeconds, 0, 3600);
            media.Volume = Math.Clamp(media.Volume, 0, 1);
            media.PlaybackRate = Math.Clamp(media.PlaybackRate <= 0 ? 1 : media.PlaybackRate, .25, 4);
            media.FadeInSeconds = Math.Clamp(media.FadeInSeconds, 0, Math.Max(0, media.TimelineLengthSeconds / 2));
            media.FadeOutSeconds = Math.Clamp(media.FadeOutSeconds, 0, Math.Max(0, media.TimelineLengthSeconds / 2));
            media.WaveformSamples ??= [];
            if (media.WaveformSamples.Count > 256) media.WaveformSamples = media.WaveformSamples.Take(256).ToList();
            var fallbackMimeType = media is VideoElement ? "video/webm" : "audio/webm";
            media.MimeType = PublicationMediaData.NormalizeMimeType(media.MimeType, fallbackMimeType);
            media.DataUrl = PublicationMediaData.NormalizeDataUrl(media.DataUrl, media.MimeType);
            if (media is VideoElement video && string.IsNullOrWhiteSpace(video.AltText))
                video.AltText = video.Name;
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


        foreach (var visual in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<DataVisualElement>())
        {
            visual.ValueFields ??= [];
            visual.RowLimit = Math.Clamp(visual.RowLimit <= 0 ? 12 : visual.RowLimit, 1, 100);
            visual.MaximumValue = visual.MaximumValue <= visual.MinimumValue ? visual.MinimumValue + 100 : visual.MaximumValue;
            visual.BorderWidthMm = Math.Clamp(visual.BorderWidthMm, 0, 8);
            var source = document.DataObjects.FirstOrDefault(data => data.Id == visual.DataObjectId);
            if (source is null && document.DataObjects.Count > 0)
                visual.DataObjectId = document.DataObjects[0].Id;
            source = document.DataObjects.FirstOrDefault(data => data.Id == visual.DataObjectId);
            var columns = _data.ResolveColumns(source);
            if (string.IsNullOrWhiteSpace(visual.ArgumentField))
                visual.ArgumentField = columns.FirstOrDefault()?.Name ?? string.Empty;
            if (visual.ValueFields.Count == 0)
            {
                var numeric = columns.FirstOrDefault(column => column.ValueKind == PublicationDataValueKind.Number)?.Name;
                if (!string.IsNullOrWhiteSpace(numeric)) visual.ValueFields.Add(numeric);
            }
        }

        foreach (var publicationPage in document.Pages)
        {
            publicationPage.Transition ??= new PublicationPageTransition();
            publicationPage.Transition.DurationSeconds = Math.Clamp(publicationPage.Transition.DurationSeconds <= 0 ? .55 : publicationPage.Transition.DurationSeconds, .1, 8);
            publicationPage.Transition.AutoAdvanceSeconds = Math.Clamp(publicationPage.Transition.AutoAdvanceSeconds <= 0 ? 5 : publicationPage.Transition.AutoAdvanceSeconds, .25, 3600);
            publicationPage.TimelineDurationSeconds = Math.Clamp(publicationPage.TimelineDurationSeconds <= 0 ? 10 : publicationPage.TimelineDurationSeconds, 1, 3600);

            var orderedElements = publicationPage.Elements
                .Select((element, index) => new { Element = element, Index = index })
                .OrderBy(item => item.Element.ZIndex)
                .ThenBy(item => item.Index)
                .Select(item => item.Element)
                .ToList();
            for (var index = 0; index < orderedElements.Count; index++) orderedElements[index].ZIndex = index + 1;

            foreach (var element in publicationPage.Elements)
            {
                element.Animations ??= [];
                element.Interaction ??= new PublicationInteraction();
            }

            var usedAnimationIds = new HashSet<Guid>();
            var timeline = publicationPage.Elements
                .SelectMany((element, elementIndex) => element.Animations.Select((animation, animationIndex) => new
                {
                    Animation = animation,
                    ElementIndex = elementIndex,
                    AnimationIndex = animationIndex
                }))
                .OrderBy(item => item.Animation.Order <= 0 ? int.MaxValue : item.Animation.Order)
                .ThenBy(item => item.ElementIndex)
                .ThenBy(item => item.AnimationIndex)
                .ToList();
            for (var index = 0; index < timeline.Count; index++)
            {
                var animation = timeline[index].Animation;
                if (animation.Id == Guid.Empty || !usedAnimationIds.Add(animation.Id))
                {
                    animation.Id = Guid.NewGuid();
                    usedAnimationIds.Add(animation.Id);
                }
                animation.Order = index + 1;
                animation.DurationSeconds = Math.Clamp(animation.DurationSeconds <= 0 ? .6 : animation.DurationSeconds, .05, 60);
                animation.DelaySeconds = Math.Clamp(animation.DelaySeconds, 0, 60);
                if (animation.TimelineStartSeconds is { } timelineStart)
                    animation.TimelineStartSeconds = Math.Clamp(timelineStart, 0, 3600);
                animation.DistancePercent = Math.Clamp(animation.DistancePercent, 0, 500);
                animation.ScalePercent = Math.Clamp(animation.ScalePercent, 0, 500);
                animation.RotationDegrees = Math.Clamp(animation.RotationDegrees, -3600, 3600);
                animation.RepeatCount = Math.Clamp(animation.RepeatCount <= 0 ? 1 : animation.RepeatCount, 1, 100);
                if (string.IsNullOrWhiteSpace(animation.Name))
                    animation.Name = $"{animation.Effect} {animation.Phase}";
            }

            var elementIds = publicationPage.Elements.Select(item => item.Id).ToHashSet();
            var objectIds = publicationPage.Elements.Where(item => item is not ConnectorElement).Select(item => item.Id).ToHashSet();
            foreach (var element in publicationPage.Elements)
            {
                if (element.Interaction.TargetElementId is { } targetId && !elementIds.Contains(targetId))
                    element.Interaction.TargetElementId = null;
                if (element.Interaction.TargetPageId is { } targetPageId && document.Pages.All(page => page.Id != targetPageId))
                    element.Interaction.TargetPageId = null;
            }
            publicationPage.Elements.RemoveAll(item => item is ConnectorElement connector &&
                (!objectIds.Contains(connector.Source.ElementId) || !objectIds.Contains(connector.Target.ElementId) || connector.Source.ElementId == connector.Target.ElementId));
            foreach (var connector in publicationPage.Elements.OfType<ConnectorElement>())
                connector.StrokeWidthMm = Math.Clamp(connector.StrokeWidthMm <= 0 ? .7 : connector.StrokeWidthMm, .1, 12);
        }

        document.FormatVersion = "1.19";
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
