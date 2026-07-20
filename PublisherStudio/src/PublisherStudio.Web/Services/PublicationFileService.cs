using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed record StoryPageLayout(
    double PageWidthMm,
    double PageHeightMm,
    double MarginTopMm,
    double MarginRightMm,
    double MarginBottomMm,
    double MarginLeftMm)
{
    public static StoryPageLayout Default { get; } = new(210, 297, 25.4, 25.4, 25.4, 25.4);

    public double ContentWidthMm => Math.Max(1, PageWidthMm - MarginLeftMm - MarginRightMm);
    public double ContentHeightMm => Math.Max(1, PageHeightMm - MarginTopMm - MarginBottomMm);
    public bool IsLandscape => PageWidthMm > PageHeightMm;

    public static StoryPageLayout Normalize(
        double pageWidthMm,
        double pageHeightMm,
        double marginTopMm,
        double marginRightMm,
        double marginBottomMm,
        double marginLeftMm)
    {
        var width = Math.Clamp(double.IsFinite(pageWidthMm) ? pageWidthMm : Default.PageWidthMm, 25.4, 2000);
        var height = Math.Clamp(double.IsFinite(pageHeightMm) ? pageHeightMm : Default.PageHeightMm, 25.4, 2000);
        var top = NormalizeMargin(marginTopMm, height);
        var right = NormalizeMargin(marginRightMm, width);
        var bottom = NormalizeMargin(marginBottomMm, height);
        var left = NormalizeMargin(marginLeftMm, width);
        NormalizePair(ref left, ref right, width);
        NormalizePair(ref top, ref bottom, height);
        return new StoryPageLayout(width, height, top, right, bottom, left);
    }

    private static double NormalizeMargin(double value, double pageSize) =>
        Math.Clamp(double.IsFinite(value) ? value : 0, 0, Math.Max(0, pageSize - 1));

    private static void NormalizePair(ref double first, ref double second, double pageSize)
    {
        var maximum = Math.Max(1, pageSize - 1);
        var sum = first + second;
        if (sum <= maximum || sum <= 0) return;
        var scale = maximum / sum;
        first *= scale;
        second *= scale;
    }
}

public sealed partial class PublicationFileService
{
    private readonly PictureDocumentService _pictures;
    private readonly PublicationDataService _data;
    private readonly SpreadsheetDocumentService _spreadsheets;
    private readonly PublicationComponentService _components;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PublicationFileService(PictureDocumentService pictures, PublicationDataService data, SpreadsheetDocumentService spreadsheets, PublicationComponentService components)
    {
        _pictures = pictures;
        _data = data;
        _spreadsheets = spreadsheets;
        _components = components;
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
            text.DocumentBackground = NormalizeCssBackground(text.DocumentBackground);
            text.PaddingMm = Math.Clamp(text.PaddingMm, 0, 50);
            text.BorderWidth = Math.Clamp(text.BorderWidth, 0, 5);
            text.ContentOffsetX = Math.Clamp(text.ContentOffsetX, -500, 500);
            text.ContentOffsetY = Math.Clamp(text.ContentOffsetY, -500, 500);
            text.ContentScale = Math.Clamp(text.ContentScale <= 0 ? 1 : text.ContentScale, .1, 12);
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
            else if (string.Equals(text.DocumentBackground, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                // v1.22 and older publications did not persist the RichEdit page color separately.
                // Recover it directly from the stored DOCX package when possible.
                text.DocumentBackground = ExtractOpenXmlDocumentBackground(text.DocumentContent);
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

        foreach (var spreadsheet in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<SpreadsheetElement>())
        {
            spreadsheet.WorkbookContent ??= [];
            if (spreadsheet.WorkbookContent.Length == 0)
            {
                spreadsheet.WorkbookContent = _spreadsheets.CreateBlankXlsx();
                spreadsheet.StorageFormat = SpreadsheetStorageFormat.Xlsx;
            }
            spreadsheet.WorkbookFileName = _spreadsheets.NormalizeWorkbookFileName(spreadsheet.WorkbookFileName, spreadsheet.StorageFormat);
            spreadsheet.BorderWidthMm = Math.Clamp(spreadsheet.BorderWidthMm, 0, 8);
            spreadsheet.ContentOffsetX = Math.Clamp(spreadsheet.ContentOffsetX, -500, 500);
            spreadsheet.ContentOffsetY = Math.Clamp(spreadsheet.ContentOffsetY, -500, 500);
            spreadsheet.ContentScale = Math.Clamp(spreadsheet.ContentScale <= 0 ? 1 : spreadsheet.ContentScale, .1, 12);
            // Never trust preview HTML stored in an externally edited publication file. Rebuild it
            // deterministically from the embedded workbook package on every load.
            spreadsheet.PreviewHtml = _spreadsheets.RenderPreviewHtml(spreadsheet.WorkbookContent, spreadsheet.StorageFormat, out var sheetName);
            spreadsheet.ActiveSheetName = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName;
            spreadsheet.Width = Math.Max(35, spreadsheet.Width);
            spreadsheet.Height = Math.Max(24, spreadsheet.Height);
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


        foreach (var component in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<DevExtremeComponentElement>())
            _components.Normalize(document, component);

        foreach (var visual in document.Pages.SelectMany(publicationPage => publicationPage.Elements).OfType<DataVisualElement>())
        {
            visual.ValueFields ??= [];
            visual.LowValueField ??= string.Empty;
            visual.HighValueField ??= string.Empty;
            visual.OpenValueField ??= string.Empty;
            visual.CloseValueField ??= string.Empty;
            visual.SizeField ??= string.Empty;
            visual.TargetField ??= string.Empty;
            visual.ParentField ??= string.Empty;
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
            var numericFields = columns.Where(column => column.ValueKind == PublicationDataValueKind.Number).Select(column => column.Name).ToArray();
            var primary = visual.ValueFields.FirstOrDefault() ?? numericFields.FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(visual.OpenValueField)) visual.OpenValueField = numericFields.ElementAtOrDefault(0) ?? primary;
            if (string.IsNullOrWhiteSpace(visual.HighValueField)) visual.HighValueField = numericFields.ElementAtOrDefault(1) ?? primary;
            if (string.IsNullOrWhiteSpace(visual.LowValueField)) visual.LowValueField = numericFields.ElementAtOrDefault(2) ?? primary;
            if (string.IsNullOrWhiteSpace(visual.CloseValueField)) visual.CloseValueField = numericFields.ElementAtOrDefault(3) ?? primary;
            if (string.IsNullOrWhiteSpace(visual.SizeField)) visual.SizeField = numericFields.ElementAtOrDefault(1) ?? primary;
            if (string.IsNullOrWhiteSpace(visual.TargetField)) visual.TargetField = columns.FirstOrDefault(column => !string.Equals(column.Name, visual.ArgumentField, StringComparison.OrdinalIgnoreCase))?.Name ?? string.Empty;
            var minimum = visual.VisualKind switch
            {
                DataVisualKind.Sparkline => (Width: 55d, Height: 18d),
                DataVisualKind.KpiProgress => (Width: 60d, Height: 24d),
                DataVisualKind.LinearGauge => (Width: 70d, Height: 24d),
                DataVisualKind.DataTable => (Width: 80d, Height: 48d),
                _ => (Width: 75d, Height: 55d)
            };
            visual.Width = Math.Max(minimum.Width, visual.Width);
            visual.Height = Math.Max(minimum.Height, visual.Height);
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
            static bool EndpointValid(ConnectorEndpoint endpoint, HashSet<Guid> ids) =>
                endpoint.Kind == ConnectorEndpointKind.Canvas ||
                (endpoint.ElementId != Guid.Empty && ids.Contains(endpoint.ElementId));

            publicationPage.Elements.RemoveAll(item => item is ConnectorElement connector &&
                (!EndpointValid(connector.Source, objectIds) ||
                 !EndpointValid(connector.Target, objectIds) ||
                 (connector.Source.Kind == ConnectorEndpointKind.Element &&
                  connector.Target.Kind == ConnectorEndpointKind.Element &&
                  connector.Source.ElementId == connector.Target.ElementId)));
            foreach (var connector in publicationPage.Elements.OfType<ConnectorElement>())
            {
                connector.Source ??= new ConnectorEndpoint();
                connector.Target ??= new ConnectorEndpoint();
                connector.Signal ??= new SignalConnectorSettings();
                connector.Source.X = Math.Clamp(connector.Source.X, 0, publicationPage.WidthMm);
                connector.Source.Y = Math.Clamp(connector.Source.Y, 0, publicationPage.HeightMm);
                connector.Target.X = Math.Clamp(connector.Target.X, 0, publicationPage.WidthMm);
                connector.Target.Y = Math.Clamp(connector.Target.Y, 0, publicationPage.HeightMm);
                connector.StrokeWidthMm = Math.Clamp(connector.StrokeWidthMm <= 0 ? .7 : connector.StrokeWidthMm, .1, 12);
                connector.Signal.DelaySeconds = Math.Clamp(connector.Signal.DelaySeconds, 0, 3600);
                connector.Signal.DurationSeconds = Math.Clamp(connector.Signal.DurationSeconds <= 0 ? 1.5 : connector.Signal.DurationSeconds, .05, 3600);
                connector.Signal.RepeatCount = Math.Clamp(connector.Signal.RepeatCount <= 0 ? 1 : connector.Signal.RepeatCount, 1, 1000);
                connector.Signal.Scale = Math.Clamp(connector.Signal.Scale <= 0 ? 1 : connector.Signal.Scale, .01, 100);
                connector.Signal.Opacity = Math.Clamp(connector.Signal.Opacity, 0, 1);
                connector.Signal.CompletionDurationSeconds = Math.Clamp(connector.Signal.CompletionDurationSeconds <= 0 ? .8 : connector.Signal.CompletionDurationSeconds, .01, 3600);
                if (connector.Signal.MotionTargetElementId is { } motionTarget && !elementIds.Contains(motionTarget))
                    connector.Signal.MotionTargetElementId = null;
                if (connector.Signal.CompletionTargetElementId is { } completionTarget && !elementIds.Contains(completionTarget))
                    connector.Signal.CompletionTargetElementId = null;
                if (connector.Signal.NextConnectorId is { } nextConnector &&
                    !publicationPage.Elements.OfType<ConnectorElement>().Any(candidate => candidate.Id == nextConnector && candidate.Signal.Enabled))
                    connector.Signal.NextConnectorId = null;
            }
        }

        document.FormatVersion = "1.39";
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

    public static string SanitizeSpreadsheetPreview(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        // Spreadsheet previews are generated by SpreadsheetDocumentService. This rejects scriptable
        // content if a publication file was edited outside PublisherStudio.
        var result = DangerousElementsRegex().Replace(html, string.Empty);
        result = EventAttributeRegex().Replace(result, string.Empty);
        result = JavascriptUrlRegex().Replace(result, "$1=\"#\"");
        return result;
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
        var styles = string.Concat(StyleRegex().Matches(html).Cast<Match>().Select(item => item.Value));
        var body = match.Success ? match.Groups[1].Value : html;
        return SanitizePreviewHtml($"{styles}<div class=\"publisher-story-document\">{body}</div>");
    }

    public static StoryPageLayout ExtractOpenXmlPageLayout(byte[] openXml)
    {
        var fallback = StoryPageLayout.Default;
        if (openXml is null || openXml.Length < 4 || openXml[0] != (byte)'P' || openXml[1] != (byte)'K')
            return fallback;

        try
        {
            using var stream = new MemoryStream(openXml, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry is null) return fallback;

            using var documentStream = documentEntry.Open();
            var document = XDocument.Load(documentStream, LoadOptions.None);
            XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var body = document.Root?.Element(word + "body");
            var section = body?.Descendants(word + "sectPr").FirstOrDefault();
            if (section is null) return fallback;

            var pageSize = section.Element(word + "pgSz");
            var width = ReadOpenXmlTwips(pageSize, word + "w", fallback.PageWidthMm);
            var height = ReadOpenXmlTwips(pageSize, word + "h", fallback.PageHeightMm);
            var orientation = pageSize?.Attribute(word + "orient")?.Value?.Trim();
            if ((string.Equals(orientation, "landscape", StringComparison.OrdinalIgnoreCase) && width < height)
                || (string.Equals(orientation, "portrait", StringComparison.OrdinalIgnoreCase) && width > height))
            {
                (width, height) = (height, width);
            }

            var pageMargins = section.Element(word + "pgMar");
            var top = ReadOpenXmlTwips(pageMargins, word + "top", fallback.MarginTopMm);
            var right = ReadOpenXmlTwips(pageMargins, word + "right", fallback.MarginRightMm);
            var bottom = ReadOpenXmlTwips(pageMargins, word + "bottom", fallback.MarginBottomMm);
            var left = ReadOpenXmlTwips(pageMargins, word + "left", fallback.MarginLeftMm);
            var gutter = ReadOpenXmlTwips(pageMargins, word + "gutter", 0);

            if (gutter > 0)
            {
                var gutterAtTop = false;
                var settingsEntry = archive.GetEntry("word/settings.xml");
                if (settingsEntry is not null)
                {
                    try
                    {
                        using var settingsStream = settingsEntry.Open();
                        var settings = XDocument.Load(settingsStream, LoadOptions.None);
                        gutterAtTop = settings.Root?.Element(word + "gutterAtTop") is not null;
                    }
                    catch (InvalidDataException) { }
                    catch (IOException) { }
                    catch (System.Xml.XmlException) { }
                }

                if (gutterAtTop) top += gutter;
                else if (section.Element(word + "rtlGutter") is not null) right += gutter;
                else left += gutter;
            }

            return StoryPageLayout.Normalize(width, height, top, right, bottom, left);
        }
        catch (InvalidDataException)
        {
            return fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
        catch (System.Xml.XmlException)
        {
            return fallback;
        }
    }

    private static double ReadOpenXmlTwips(XElement? element, XName attributeName, double fallbackMillimeters)
    {
        var value = element?.Attribute(attributeName)?.Value;
        return long.TryParse(value, out var twips)
            ? twips * 25.4d / 1440d
            : fallbackMillimeters;
    }

    public static string ExtractOpenXmlDocumentBackground(byte[] openXml)
    {
        if (openXml is null || openXml.Length < 4 || openXml[0] != (byte)'P' || openXml[1] != (byte)'K')
            return "transparent";

        try
        {
            using var stream = new MemoryStream(openXml, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry is null) return "transparent";

            using var documentStream = documentEntry.Open();
            var document = XDocument.Load(documentStream, LoadOptions.None);
            XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var background = document.Root?.Element(word + "background");
            var color = background?.Attribute(word + "color")?.Value;
            if (string.IsNullOrWhiteSpace(color) || string.Equals(color, "auto", StringComparison.OrdinalIgnoreCase))
                return "transparent";

            var normalized = color.Trim();
            if (Regex.IsMatch(normalized, "^[0-9a-fA-F]{6}$"))
                normalized = "#" + normalized;
            else if (Regex.IsMatch(normalized, "^[0-9a-fA-F]{3}$"))
                normalized = "#" + normalized;

            return NormalizeCssBackground(normalized);
        }
        catch (InvalidDataException)
        {
            return "transparent";
        }
        catch (IOException)
        {
            return "transparent";
        }
        catch (System.Xml.XmlException)
        {
            return "transparent";
        }
    }



    public static bool IsOpenXmlDocument(byte[] content)
    {
        if (content is null || content.Length < 4 || content[0] != (byte)'P' || content[1] != (byte)'K')
            return false;
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            return archive.GetEntry("word/document.xml") is not null
                && archive.GetEntry("[Content_Types].xml") is not null;
        }
        catch (InvalidDataException) { return false; }
        catch (IOException) { return false; }
    }

    public static string CreateOpenXmlPreviewHtml(byte[] openXml, string? fallbackTitle = null)
    {
        var safeTitle = System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(fallbackTitle) ? "Imported Word document" : fallbackTitle);
        if (!IsOpenXmlDocument(openXml))
            return $"<p style=\"margin:0;font:600 12pt Segoe UI;color:#9f1239\">{safeTitle} is not a valid DOCX document.</p>";

        try
        {
            using var stream = new MemoryStream(openXml, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var documentEntry = archive.GetEntry("word/document.xml")!;
            using var documentStream = documentEntry.Open();
            var document = XDocument.Load(documentStream, LoadOptions.None);
            XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var body = document.Root?.Element(word + "body");
            if (body is null)
                return $"<p style=\"margin:0;font:600 12pt Segoe UI\">{safeTitle}</p>";

            var html = new System.Text.StringBuilder();
            foreach (var child in body.Elements())
            {
                if (child.Name == word + "p")
                    html.Append(RenderOpenXmlParagraph(child, word));
                else if (child.Name == word + "tbl")
                    html.Append(RenderOpenXmlTable(child, word));
            }

            if (html.Length == 0)
                html.Append($"<p style=\"margin:0;font:600 12pt Segoe UI\">{safeTitle}</p><p style=\"margin:6px 0 0;color:#526071\">Double-click to open the complete DOCX in Story Editor.</p>");

            var background = NormalizeCssBackground(ExtractOpenXmlDocumentBackground(openXml));
            var fill = IsVisibleCssBackground(background)
                ? $" data-publisher-print-fill=\"true\" style=\"--publisher-story-page-background:{background};--publisher-print-fill:{background};background-color:{background}\""
                : string.Empty;
            return SanitizePreviewHtml($"<div class=\"publisher-story-document\"{fill}>{html}</div>");
        }
        catch (InvalidDataException)
        {
            return $"<p style=\"margin:0;font:600 12pt Segoe UI\">{safeTitle}</p><p style=\"margin:6px 0 0;color:#9f1239\">The DOCX preview could not be read. The original document remains available in Story Editor.</p>";
        }
        catch (IOException)
        {
            return $"<p style=\"margin:0;font:600 12pt Segoe UI\">{safeTitle}</p><p style=\"margin:6px 0 0;color:#9f1239\">The DOCX preview could not be read. The original document remains available in Story Editor.</p>";
        }
        catch (System.Xml.XmlException)
        {
            return $"<p style=\"margin:0;font:600 12pt Segoe UI\">{safeTitle}</p><p style=\"margin:6px 0 0;color:#9f1239\">The DOCX preview could not be read. The original document remains available in Story Editor.</p>";
        }
    }

    private static string RenderOpenXmlTable(XElement table, XNamespace word)
    {
        var builder = new System.Text.StringBuilder("<table style=\"width:100%;border-collapse:collapse;margin:4px 0 10px\"><tbody>");
        foreach (var row in table.Elements(word + "tr"))
        {
            builder.Append("<tr>");
            foreach (var cell in row.Elements(word + "tc"))
            {
                builder.Append("<td style=\"vertical-align:top;border:1px solid #cbd5e1;padding:4px 6px\">");
                foreach (var paragraph in cell.Elements(word + "p"))
                    builder.Append(RenderOpenXmlParagraph(paragraph, word));
                builder.Append("</td>");
            }
            builder.Append("</tr>");
        }
        builder.Append("</tbody></table>");
        return builder.ToString();
    }

    private static string RenderOpenXmlParagraph(XElement paragraph, XNamespace word)
    {
        var properties = paragraph.Element(word + "pPr");
        var styleName = properties?.Element(word + "pStyle")?.Attribute(word + "val")?.Value ?? string.Empty;
        var headingLevel = Regex.Match(styleName, @"(?:heading|überschrift)\s*([1-6])", RegexOptions.IgnoreCase);
        var tag = headingLevel.Success ? $"h{headingLevel.Groups[1].Value}" : "p";
        var css = new List<string> { "margin:0 0 7px" };
        var alignment = properties?.Element(word + "jc")?.Attribute(word + "val")?.Value;
        if (!string.IsNullOrWhiteSpace(alignment))
        {
            var mapped = alignment.ToLowerInvariant() switch
            {
                "center" => "center",
                "right" or "end" => "right",
                "both" or "distribute" => "justify",
                _ => "left"
            };
            css.Add($"text-align:{mapped}");
        }

        var builder = new System.Text.StringBuilder();
        if (properties?.Element(word + "numPr") is not null)
            builder.Append("<span aria-hidden=\"true\" style=\"display:inline-block;width:1.2em\">•</span>");

        foreach (var run in paragraph.Descendants(word + "r"))
            builder.Append(RenderOpenXmlRun(run, word));

        if (paragraph.Descendants(word + "drawing").Any() || paragraph.Descendants(word + "pict").Any())
            builder.Append("<span style=\"display:inline-block;padding:3px 6px;border:1px dashed #94a3b8;color:#64748b;background:#f8fafc\">Embedded picture — open Story Editor for full fidelity</span>");

        var content = builder.Length == 0 ? "<br>" : builder.ToString();
        return $"<{tag} style=\"{string.Join(';', css)}\">{content}</{tag}>";
    }

    private static string RenderOpenXmlRun(XElement run, XNamespace word)
    {
        var text = new System.Text.StringBuilder();
        foreach (var child in run.Elements())
        {
            if (child.Name == word + "t" || child.Name == word + "instrText")
                text.Append(System.Net.WebUtility.HtmlEncode(child.Value));
            else if (child.Name == word + "tab")
                text.Append("&emsp;");
            else if (child.Name == word + "br" || child.Name == word + "cr")
                text.Append("<br>");
            else if (child.Name == word + "noBreakHyphen")
                text.Append("&#8209;");
        }
        if (text.Length == 0) return string.Empty;

        var properties = run.Element(word + "rPr");
        if (properties is null) return text.ToString();
        var css = new List<string>();
        if (properties.Element(word + "b") is not null) css.Add("font-weight:700");
        if (properties.Element(word + "i") is not null) css.Add("font-style:italic");
        if (properties.Element(word + "strike") is not null) css.Add("text-decoration:line-through");
        var underline = properties.Element(word + "u")?.Attribute(word + "val")?.Value;
        if (!string.IsNullOrWhiteSpace(underline) && !string.Equals(underline, "none", StringComparison.OrdinalIgnoreCase))
            css.Add("text-decoration:underline");
        var color = NormalizeOpenXmlColor(properties.Element(word + "color")?.Attribute(word + "val")?.Value);
        if (color is not null) css.Add($"color:{color}");
        var highlight = OpenXmlHighlightColor(properties.Element(word + "highlight")?.Attribute(word + "val")?.Value)
            ?? NormalizeOpenXmlColor(properties.Element(word + "shd")?.Attribute(word + "fill")?.Value);
        if (highlight is not null)
        {
            css.Add($"background-color:{highlight}");
            css.Add($"--publisher-print-fill:{highlight}");
        }
        var sizeValue = properties.Element(word + "sz")?.Attribute(word + "val")?.Value;
        if (double.TryParse(sizeValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var halfPoints) && halfPoints > 0)
            css.Add($"font-size:{(halfPoints / 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}pt");
        var fonts = properties.Element(word + "rFonts");
        var font = fonts?.Attribute(word + "ascii")?.Value ?? fonts?.Attribute(word + "hAnsi")?.Value;
        if (!string.IsNullOrWhiteSpace(font)) css.Add($"font-family:'{System.Net.WebUtility.HtmlEncode(font)}'");
        var vertical = properties.Element(word + "vertAlign")?.Attribute(word + "val")?.Value;
        if (string.Equals(vertical, "superscript", StringComparison.OrdinalIgnoreCase)) css.Add("vertical-align:super;font-size:.75em");
        if (string.Equals(vertical, "subscript", StringComparison.OrdinalIgnoreCase)) css.Add("vertical-align:sub;font-size:.75em");
        if (css.Count == 0) return text.ToString();
        var printFill = highlight is not null ? " data-publisher-print-fill=\"true\"" : string.Empty;
        return $"<span{printFill} style=\"{string.Join(';', css)}\">{text}</span>";
    }

    private static string? NormalizeOpenXmlColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase)) return null;
        var normalized = value.Trim();
        return Regex.IsMatch(normalized, "^[0-9a-fA-F]{6}$") ? "#" + normalized : null;
    }

    private static string? OpenXmlHighlightColor(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "black" => "#000000", "blue" => "#0000ff", "cyan" => "#00ffff", "green" => "#008000",
        "magenta" => "#ff00ff", "red" => "#ff0000", "yellow" => "#ffff00", "white" => "#ffffff",
        "darkblue" => "#000080", "darkcyan" => "#008080", "darkgreen" => "#006400", "darkmagenta" => "#800080",
        "darkred" => "#800000", "darkyellow" => "#808000", "darkgray" => "#808080", "lightgray" => "#d3d3d3",
        _ => null
    };

    private static bool IsVisibleCssBackground(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !string.Equals(value, "transparent", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(value, "none", StringComparison.OrdinalIgnoreCase);

    public static string NormalizeCssBackground(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "transparent";
        var normalized = value.Trim();
        if (normalized.Length > 512) return "transparent";
        if (normalized.IndexOfAny(new[] { ';', '"', '\'', '<', '>', '{', '}' }) >= 0
            || normalized.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("expression(", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("url(", StringComparison.OrdinalIgnoreCase))
            return "transparent";
        return normalized;
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
    [GeneratedRegex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleRegex();
    [GeneratedRegex(@"<(script|iframe|object|embed|form|input|button|meta|link)[^>]*>[\s\S]*?</\1\s*>|<(script|iframe|object|embed|form|input|button|meta|link)[^>]*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousElementsRegex();
    [GeneratedRegex("\\s+on[a-z]+\\s*=\\s*(?:\\\"[^\\\"]*\\\"|'[^']*'|[^\\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventAttributeRegex();
    [GeneratedRegex("(href|src)\\s*=\\s*[\\\"']?\\s*javascript:[^\\s>\\\"']*", RegexOptions.IgnoreCase)]
    private static partial Regex JavascriptUrlRegex();
}
