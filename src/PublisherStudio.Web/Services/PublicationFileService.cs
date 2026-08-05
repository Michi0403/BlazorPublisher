using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.MediaStudio.UseCases;
using PublisherStudio.Services.Panels;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Provides publication file service operations.
/// </summary>
public sealed partial class PublicationFileService
{
    private readonly PictureDocumentService _pictures;
    private readonly PublicationDataService _data;
    private readonly SpreadsheetDocumentService _spreadsheets;
    private readonly PublicationComponentService _components;
    private readonly MediaTimelineEditService _mediaTimeline;
    private readonly PanelDocumentService _panels;
    private readonly WordArtPathGeometry _wordArtGeometry;
    private readonly PublicationElementTraversal _elementTraversal;
    private readonly PublicationMediaData _mediaData;
    private readonly RichTextDocumentFactory _richTextFactory;
    private readonly IPublisherRuntimePatternService _runtimePatterns;
    private readonly IPublisherDocumentFactory _documentFactory;
    private readonly IPublicationMarkupService _markup;
    private readonly IStoryPageLayoutService _storyPageLayouts;
    private readonly ILogger<PublicationFileService> logger;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Runs the publication file service operation.
    /// </summary>
    public PublicationFileService(
        PictureDocumentService pictures,
        PublicationDataService data,
        SpreadsheetDocumentService spreadsheets,
        PublicationComponentService components,
        MediaTimelineEditService mediaTimeline,
        PanelDocumentService panels,
        WordArtPathGeometry wordArtGeometry,
        PublicationElementTraversal elementTraversal,
        PublicationMediaData mediaData,
        RichTextDocumentFactory richTextFactory,
        IPublisherRuntimePatternService runtimePatterns,
        IPublisherDocumentFactory documentFactory,
        IPublicationMarkupService markup,
        IStoryPageLayoutService storyPageLayouts,
        ILogger<PublicationFileService> logger)
    {
        _pictures = pictures;
        _data = data;
        _spreadsheets = spreadsheets;
        _components = components;
        _mediaTimeline = mediaTimeline;
        _panels = panels;
        _wordArtGeometry = wordArtGeometry;
        _elementTraversal = elementTraversal;
        _mediaData = mediaData;
        _richTextFactory = richTextFactory;
        _runtimePatterns = runtimePatterns;
        _documentFactory = documentFactory;
        _markup = markup;
        _storyPageLayouts = storyPageLayouts;
        this.logger = logger;
    }

    /// <summary>
    /// Runs the serialize operation.
    /// </summary>
    public string Serialize(PublicationDocument document)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.Serialize.");
                    document.ModifiedUtc = DateTimeOffset.UtcNow;
                    var root = JsonSerializer.SerializeToNode(document, _options) as JsonObject
                        ?? throw new InvalidDataException("The publication could not be serialized.");
                    var streamingProperty = _options.PropertyNamingPolicy?.ConvertName(nameof(PublicationDocument.Streaming))
                        ?? nameof(PublicationDocument.Streaming);
                    root.Remove(streamingProperty);
                    return root.ToJsonString(_options);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.Serialize failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether embedded streaming settings.
    /// </summary>
    public bool HasEmbeddedStreamingSettings(string json)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.HasEmbeddedStreamingSettings.");
                    if (string.IsNullOrWhiteSpace(json)) return false;
                    try
                    {
                        using var parsed = JsonDocument.Parse(json);
                        if (parsed.RootElement.ValueKind != JsonValueKind.Object) return false;
                        return parsed.RootElement.EnumerateObject().Any(property =>
                            string.Equals(property.Name, nameof(PublicationDocument.Streaming), StringComparison.OrdinalIgnoreCase));
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.HasEmbeddedStreamingSettings failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes streaming settings.
    /// </summary>
    public void NormalizeStreamingSettings(PublicationDocument document) {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.NormalizeStreamingSettings.");
            NormalizeStreaming(document);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.NormalizeStreamingSettings failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the clone element operation.
    /// </summary>
    public PublicationElement CloneElement(PublicationElement element) {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.CloneElement.");
            return JsonSerializer.Deserialize<PublicationElement>(JsonSerializer.Serialize<PublicationElement>(element, _options), _options)
        ?? throw new InvalidDataException("The publication element could not be cloned.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.CloneElement failed: {exception.Message}");
            throw;
        }
    }


    /// <summary>
    /// Runs the serialize element operation.
    /// </summary>
    public string SerializeElement(PublicationElement element) {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.SerializeElement.");
            return JsonSerializer.Serialize<PublicationElement>(element, _options);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.SerializeElement failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the deserialize element operation.
    /// </summary>
    public PublicationElement DeserializeElement(string json) {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.DeserializeElement.");
            return JsonSerializer.Deserialize<PublicationElement>(json, _options)
        ?? throw new InvalidDataException("The component configuration is empty or invalid.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.DeserializeElement failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the clone page operation.
    /// </summary>
    public PublicationPage ClonePage(PublicationPage publicationPage) {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.ClonePage.");
            return JsonSerializer.Deserialize<PublicationPage>(JsonSerializer.Serialize(publicationPage, _options), _options)
        ?? throw new InvalidDataException("The publication page could not be cloned.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.ClonePage failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the deserialize operation.
    /// </summary>
    public PublicationDocument Deserialize(string json)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.Deserialize.");
                    var document = JsonSerializer.Deserialize<PublicationDocument>(json, _options)
                        ?? throw new InvalidDataException("The publication file is empty or invalid.");
                    document.View ??= new PublicationViewSettings();
                    document.Playback ??= new PublicationPlaybackSettings();
                    document.Streaming ??= new PublicationStreamingSettings();
                    document.Pages ??= [];
                    document.DataObjects ??= [];
                    document.ComponentTemplates ??= [];
                    if (document.Pages.Count == 0)
                        document.Pages.Add(_documentFactory.CreatePage());
                    foreach (var publicationPage in document.Pages)
                        publicationPage.Elements ??= [];
                    _data.Normalize(document);
                    foreach (var panel in document.Pages.SelectMany(page => page.Elements).OfType<PanelElement>())
                        _panels.Normalize(document, panel);
                    foreach (var template in document.ComponentTemplates)
                        _panels.NormalizeTemplate(document, template);
                    NormalizeStreaming(document);
                    var allElements = _elementTraversal.Descendants(document).ToArray();
                    document.Zoom = Math.Clamp(Math.Round((document.Zoom <= 0 ? .8 : document.Zoom) * 100d, MidpointRounding.AwayFromZero) / 100d, .2, 4);
                    document.View.GridSpacingMm = Math.Clamp(document.View.GridSpacingMm <= 0 ? 5 : document.View.GridSpacingMm, .5, 100);
                    document.View.ExportDpi = Math.Clamp(document.View.ExportDpi <= 0 ? 150 : document.View.ExportDpi, 72, 600);
                    if (!Enum.IsDefined(document.View.CanvasZoomMode)) document.View.CanvasZoomMode = PublicationCanvasZoomMode.CssLayout;
                    foreach (var text in allElements.OfType<TextFrameElement>())
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
                            text.DocumentContent = _richTextFactory.CreateOpenXml("Text frame");
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

                    foreach (var image in allElements.OfType<ImageFrameElement>())
                    {
                        if (string.IsNullOrWhiteSpace(image.OriginalDataUrl)) image.OriginalDataUrl = image.DataUrl;
                        image.Opacity = Math.Clamp(image.Opacity, 0, 1);
                        image.TintOpacity = Math.Clamp(image.TintOpacity, 0, 1);
                        image.TransparentColorTolerance = Math.Clamp(image.TransparentColorTolerance, 0, 255);
                        if (image.PictureSource is not null)
                            _pictures.Normalize(image.PictureSource);
                    }

                    foreach (var media in allElements.OfType<PublicationMediaElement>())
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
                        media.MimeType = _mediaData.NormalizeMimeType(media.MimeType, fallbackMimeType);
                        media.DataUrl = _mediaData.NormalizeDataUrl(media.DataUrl, media.MimeType);
                        media.Segments = _mediaTimeline.Normalize(media.Segments, media is VideoElement);
                        if (media is VideoElement video)
                        {
                            if (video.VideoProject is { Tracks.Count: > 0 } project)
                            {
                                video.VideoProject = _mediaTimeline.CloneVideoProject(project);
                                video.VideoProject.FrameRate = Math.Clamp(video.VideoProject.FrameRate <= 0 ? 30 : video.VideoProject.FrameRate, 1, 240);
                                video.VideoProject.Width = Math.Clamp(video.VideoProject.Width <= 0 ? 1920 : video.VideoProject.Width, 16, 16384);
                                video.VideoProject.Height = Math.Clamp(video.VideoProject.Height <= 0 ? 1080 : video.VideoProject.Height, 16, 16384);
                                foreach (var track in video.VideoProject.Tracks)
                                {
                                    track.Name = string.IsNullOrWhiteSpace(track.Name) ? $"{track.Kind} track" : track.Name.Trim();
                                    track.Segments = _mediaTimeline.Normalize(track.Segments, track.Kind == MediaTimelineTrackKind.Video);
                                }
                                var activeTrack = video.VideoProject.Tracks.FirstOrDefault(track => track.Id == video.VideoProject.ActiveTrackId && track.Kind == MediaTimelineTrackKind.Video)
                                    ?? video.VideoProject.Tracks.OrderBy(track => track.Order).FirstOrDefault(track => track.Kind == MediaTimelineTrackKind.Video)
                                    ?? video.VideoProject.Tracks.OrderBy(track => track.Order).FirstOrDefault();
                                video.VideoProject.ActiveTrackId = activeTrack?.Id ?? Guid.Empty;
                                if (media.Segments.Count == 0 && activeTrack is not null)
                                    media.Segments = _mediaTimeline.Normalize(_mediaTimeline.CreateTrackProjection(video.VideoProject, activeTrack.Id), video: true);
                            }
                            if (string.IsNullOrWhiteSpace(video.AltText)) video.AltText = video.Name;
                            video.FrameClipPolygon ??= [];
                            if (video.FrameClipPolygon.Count > 256) video.FrameClipPolygon = video.FrameClipPolygon.Take(256).ToList();
                            foreach (var point in video.FrameClipPolygon)
                            {
                                point.X = Math.Clamp(point.X, 0, 1);
                                point.Y = Math.Clamp(point.Y, 0, 1);
                            }
                            if (video.FrameClipPolygon.Count is > 0 and < 3) video.FrameClipPolygon.Clear();

                            // v1.0.71 and older stored one legacy frame polygon on the element. Migrate it into
                            // the canonical selected clip/layer model, then keep the legacy field as a projection
                            // for older render/export paths that have not yet moved to the live layer renderer.
                            var firstLayer = media.Segments.FirstOrDefault()?.VideoLayers.FirstOrDefault();
                            if (firstLayer is not null && firstLayer.Region.Points.Count < 3 && video.FrameClipPolygon.Count >= 3)
                            {
                                firstLayer.Region.Points = video.FrameClipPolygon
                                    .Select(point => new MediaFramePoint { X = point.X, Y = point.Y })
                                    .ToList();
                            }
                            video.FrameClipPolygon = firstLayer?.Region.Points is { Count: >= 3 } points
                                ? points.Select(point => new MediaFramePoint { X = point.X, Y = point.Y }).ToList()
                                : [];
                        }
                    }

                    foreach (var spreadsheet in allElements.OfType<SpreadsheetElement>())
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

                    foreach (var wordArt in allElements.OfType<WordArtElement>())
                    {
                        wordArt.FontSizePt = Math.Clamp(wordArt.FontSizePt, 6, 300);
                        wordArt.OutlineWidth = Math.Clamp(wordArt.OutlineWidth, 0, 20);
                        wordArt.ExtrudeDepth = Math.Clamp(wordArt.ExtrudeDepth, 0, 24);
                        wordArt.CustomPathPoints = _wordArtGeometry.Normalize(wordArt.CustomPathPoints);
                        wordArt.PathStartOffsetPercent = Math.Clamp(wordArt.PathStartOffsetPercent, 0, 100);
                        wordArt.PathBaselineOffset = Math.Clamp(wordArt.PathBaselineOffset, -80, 80);
                        if (!Enum.IsDefined(wordArt.FillKind))
                            wordArt.FillKind = wordArt.GradientFill ? WordArtFillKind.Gradient : WordArtFillKind.Solid;
                        // Publications created before the explicit fill enum only persisted GradientFill.
                        if (wordArt.FillKind == WordArtFillKind.Solid && wordArt.GradientFill && string.IsNullOrWhiteSpace(wordArt.FillMediaDataUrl))
                            wordArt.FillKind = WordArtFillKind.Gradient;
                        wordArt.FillMediaScale = Math.Clamp(wordArt.FillMediaScale <= 0 ? 1 : wordArt.FillMediaScale, .1, 10);
                        wordArt.FillMediaOffsetXPercent = Math.Clamp(wordArt.FillMediaOffsetXPercent, -100, 100);
                        wordArt.FillMediaOffsetYPercent = Math.Clamp(wordArt.FillMediaOffsetYPercent, -100, 100);
                        if (!Enum.IsDefined(wordArt.FillMediaFitMode)) wordArt.FillMediaFitMode = PublicationVideoFitMode.Cover;

                        var expectedPrefix = wordArt.FillKind == WordArtFillKind.Picture ? "data:image/" :
                            wordArt.FillKind == WordArtFillKind.Video ? "data:video/" : string.Empty;
                        if (!string.IsNullOrEmpty(expectedPrefix) && !wordArt.FillMediaDataUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            wordArt.FillMediaDataUrl = string.Empty;
                            wordArt.FillMediaMimeType = string.Empty;
                            wordArt.FillKind = WordArtFillKind.Solid;
                        }
                        else if (wordArt.FillKind is WordArtFillKind.Picture or WordArtFillKind.Video)
                        {
                            var fallback = wordArt.FillKind == WordArtFillKind.Picture ? "image/png" : "video/webm";
                            wordArt.FillMediaMimeType = _mediaData.NormalizeMimeType(wordArt.FillMediaMimeType, fallback);
                            wordArt.FillMediaDataUrl = _mediaData.NormalizeDataUrl(wordArt.FillMediaDataUrl, wordArt.FillMediaMimeType);
                        }
                        if (!wordArt.FillMediaPosterDataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                            wordArt.FillMediaPosterDataUrl = string.Empty;
                        wordArt.GradientFill = wordArt.FillKind == WordArtFillKind.Gradient;
                    }


                    foreach (var html in allElements.OfType<HtmlEmbedElement>())
                    {
                        html.Html ??= string.Empty;
                        html.Css ??= string.Empty;
                        html.JavaScript ??= string.Empty;
                        html.HtmlExportNote ??= html.HtmlExportSupport == PublicationHtmlExportSupport.Native
                            ? "Native HTML content."
                            : "Check HTML export compatibility before publishing.";
                        html.InterchangeFormat ??= string.Empty;
                        if (!Enum.IsDefined(html.HtmlExportSupport)) html.HtmlExportSupport = PublicationHtmlExportSupport.Native;
                        html.Background = NormalizeCssBackground(html.Background);
                    }

                    foreach (var component in allElements.OfType<DevExtremeComponentElement>())
                        _components.Normalize(document, component);

                    foreach (var visual in allElements.OfType<DataVisualElement>())
                    {
                        visual.ValueFields ??= [];
                        if (!Enum.IsDefined(visual.ArgumentMode)) visual.ArgumentMode = DataVisualArgumentMode.Auto;
                        if (!Enum.IsDefined(visual.AggregationMode)) visual.AggregationMode = DataVisualAggregationMode.Auto;
                        if (!Enum.IsDefined(visual.SortMode)) visual.SortMode = DataVisualSortMode.DataOrder;
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
                            element.ConnectorPorts ??= [];
                            var usedPortIds = new HashSet<Guid>();
                            foreach (var port in element.ConnectorPorts)
                            {
                                if (port.Id == Guid.Empty || !usedPortIds.Add(port.Id))
                                {
                                    port.Id = Guid.NewGuid();
                                    usedPortIds.Add(port.Id);
                                }
                                port.Name = string.IsNullOrWhiteSpace(port.Name) ? "Connector point" : port.Name.Trim();
                                port.XPercent = Math.Clamp(port.XPercent, 0, 1);
                                port.YPercent = Math.Clamp(port.YPercent, 0, 1);
                            }
                            if (element.Interaction.TargetElementId is { } targetId && !elementIds.Contains(targetId))
                                element.Interaction.TargetElementId = null;
                            if (element.Interaction.TargetPageId is { } targetPageId && document.Pages.All(page => page.Id != targetPageId))
                                element.Interaction.TargetPageId = null;
                        }
                        bool EndpointValid(ConnectorEndpoint endpoint, HashSet<Guid> ids) =>
                            endpoint.Kind == ConnectorEndpointKind.Canvas ||
                            (endpoint.ElementId != Guid.Empty && ids.Contains(endpoint.ElementId));

                        void NormalizeEndpointPort(ConnectorEndpoint endpoint)
                        {
                            if (endpoint.Kind != ConnectorEndpointKind.Element || endpoint.PortId is not { } portId) return;
                            var owner = publicationPage.Elements.FirstOrDefault(element => element.Id == endpoint.ElementId && element is not ConnectorElement);
                            if (owner is null || owner.ConnectorPorts.All(port => port.Id != portId)) endpoint.PortId = null;
                        }
                        double? NormalizeControl(double? value, double maximum) =>
                            value is { } coordinate && double.IsFinite(coordinate) ? Math.Clamp(coordinate, 0, maximum) : null;

                        foreach (var connector in publicationPage.Elements.OfType<ConnectorElement>())
                        {
                            connector.Source ??= new ConnectorEndpoint();
                            connector.Target ??= new ConnectorEndpoint();
                            connector.Signal ??= new SignalConnectorSettings();
                        }

                        publicationPage.Elements.RemoveAll(item => item is ConnectorElement connector &&
                            (!EndpointValid(connector.Source, objectIds) ||
                             !EndpointValid(connector.Target, objectIds) ||
                             (connector.Source.Kind == ConnectorEndpointKind.Element &&
                              connector.Target.Kind == ConnectorEndpointKind.Element &&
                              connector.Source.ElementId == connector.Target.ElementId)));
                        foreach (var connector in publicationPage.Elements.OfType<ConnectorElement>())
                        {
                            connector.Source.X = Math.Clamp(connector.Source.X, 0, publicationPage.WidthMm);
                            connector.Source.Y = Math.Clamp(connector.Source.Y, 0, publicationPage.HeightMm);
                            connector.Target.X = Math.Clamp(connector.Target.X, 0, publicationPage.WidthMm);
                            connector.Target.Y = Math.Clamp(connector.Target.Y, 0, publicationPage.HeightMm);
                            NormalizeEndpointPort(connector.Source);
                            NormalizeEndpointPort(connector.Target);
                            connector.Control1X = NormalizeControl(connector.Control1X, publicationPage.WidthMm);
                            connector.Control1Y = NormalizeControl(connector.Control1Y, publicationPage.HeightMm);
                            connector.Control2X = NormalizeControl(connector.Control2X, publicationPage.WidthMm);
                            connector.Control2Y = NormalizeControl(connector.Control2Y, publicationPage.HeightMm);
                            connector.StrokeWidthMm = Math.Clamp(connector.StrokeWidthMm <= 0 ? .7 : connector.StrokeWidthMm, .1, 12);
                            connector.Signal.DelaySeconds = Math.Clamp(connector.Signal.DelaySeconds, 0, 3600);
                            connector.Signal.DurationSeconds = Math.Clamp(connector.Signal.DurationSeconds <= 0 ? 1.5 : connector.Signal.DurationSeconds, .05, 3600);
                            connector.Signal.RepeatCount = Math.Clamp(connector.Signal.RepeatCount <= 0 ? 1 : connector.Signal.RepeatCount, 1, 1000);
                            connector.Signal.Scale = Math.Clamp(connector.Signal.Scale <= 0 ? 1 : connector.Signal.Scale, .01, 100);
                            connector.Signal.ResizeWidthPercent = Math.Clamp(connector.Signal.ResizeWidthPercent <= 0 ? 100 : connector.Signal.ResizeWidthPercent, .01, 10000);
                            connector.Signal.ResizeHeightPercent = Math.Clamp(connector.Signal.ResizeHeightPercent <= 0 ? 100 : connector.Signal.ResizeHeightPercent, .01, 10000);
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

                    document.FormatVersion = "1.55";
                    return document;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.Deserialize failed: {exception.Message}");
            throw;
        }
    }

    private bool LooksLikeHtml(byte[] content)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.LooksLikeHtml.");
                    var prefix = System.Text.Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, 128)).TrimStart();
                    return prefix.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                        || prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
                        || prefix.StartsWith("<body", StringComparison.OrdinalIgnoreCase)
                        || prefix.StartsWith("<p", StringComparison.OrdinalIgnoreCase)
                        || prefix.StartsWith("<div", StringComparison.OrdinalIgnoreCase);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.LooksLikeHtml failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the sanitize spreadsheet preview operation.
    /// </summary>
    public string SanitizeSpreadsheetPreview(string? html)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.SanitizeSpreadsheetPreview.");
                    if (string.IsNullOrWhiteSpace(html)) return string.Empty;
                    // Spreadsheet previews are generated by SpreadsheetDocumentService. This rejects scriptable
                    // content if a publication file was edited outside PublisherStudio.
                    var result = _runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationDangerousElements).Replace(html, string.Empty);
                    result = _runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationEventAttribute).Replace(result, string.Empty);
                    result = _runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationJavascriptUrl).Replace(result, "$1=\"#\"");
                    return result;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.SanitizeSpreadsheetPreview failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the safe file name operation.
    /// </summary>
    public string SafeFileName(string value)
    {
        try
        {
            logger.LogTrace($"Delegating publication file-name sanitization.");
            return _markup.SafeFileName(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not sanitize a publication file name: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the extract HTML body operation.
    /// </summary>
    public string ExtractHtmlBody(byte[] htmlBytes)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.ExtractHtmlBody.");
                    var html = System.Text.Encoding.UTF8.GetString(htmlBytes);
                    var match = _runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationBody).Match(html);
                    var styles = string.Concat(_runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationStyle).Matches(html).Cast<Match>().Select(item => item.Value));
                    var body = match.Success ? match.Groups[1].Value : html;
                    return SanitizePreviewHtml($"{styles}<div class=\"publisher-story-document\">{body}</div>");
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.ExtractHtmlBody failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the extract open XML page layout operation.
    /// </summary>
    public StoryPageLayout ExtractOpenXmlPageLayout(byte[] openXml)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.ExtractOpenXmlPageLayout.");
                    var fallback = _storyPageLayouts.GetDefault();
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

                        return _storyPageLayouts.Normalize(width, height, top, right, bottom, left);
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.ExtractOpenXmlPageLayout failed: {exception.Message}");
            throw;
        }
    }

    private double ReadOpenXmlTwips(XElement? element, XName attributeName, double fallbackMillimeters)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.ReadOpenXmlTwips.");
                    var value = element?.Attribute(attributeName)?.Value;
                    return long.TryParse(value, out var twips)
                        ? twips * 25.4d / 1440d
                        : fallbackMillimeters;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.ReadOpenXmlTwips failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the extract open XML document background operation.
    /// </summary>
    public string ExtractOpenXmlDocumentBackground(byte[] openXml)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.ExtractOpenXmlDocumentBackground.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.ExtractOpenXmlDocumentBackground failed: {exception.Message}");
            throw;
        }
    }



    /// <summary>
    /// Determines whether open XML document.
    /// </summary>
    public bool IsOpenXmlDocument(byte[] content)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.IsOpenXmlDocument.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.IsOpenXmlDocument failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates open XML preview HTML.
    /// </summary>
    public string CreateOpenXmlPreviewHtml(byte[] openXml, string? fallbackTitle = null)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.CreateOpenXmlPreviewHtml.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.CreateOpenXmlPreviewHtml failed: {exception.Message}");
            throw;
        }
    }

    private string RenderOpenXmlTable(XElement table, XNamespace word)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.RenderOpenXmlTable.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.RenderOpenXmlTable failed: {exception.Message}");
            throw;
        }
    }

    private string RenderOpenXmlParagraph(XElement paragraph, XNamespace word)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.RenderOpenXmlParagraph.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.RenderOpenXmlParagraph failed: {exception.Message}");
            throw;
        }
    }

    private string RenderOpenXmlRun(XElement run, XNamespace word)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.RenderOpenXmlRun.");
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
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.RenderOpenXmlRun failed: {exception.Message}");
            throw;
        }
    }

    private string? NormalizeOpenXmlColor(string? value)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.NormalizeOpenXmlColor.");
                    if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase)) return null;
                    var normalized = value.Trim();
                    return Regex.IsMatch(normalized, "^[0-9a-fA-F]{6}$") ? "#" + normalized : null;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.NormalizeOpenXmlColor failed: {exception.Message}");
            throw;
        }
    }

    private string? OpenXmlHighlightColor(string? value) {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.OpenXmlHighlightColor.");
            return value?.Trim().ToLowerInvariant() switch
    {
        "black" => "#000000", "blue" => "#0000ff", "cyan" => "#00ffff", "green" => "#008000",
        "magenta" => "#ff00ff", "red" => "#ff0000", "yellow" => "#ffff00", "white" => "#ffffff",
        "darkblue" => "#000080", "darkcyan" => "#008080", "darkgreen" => "#006400", "darkmagenta" => "#800080",
        "darkred" => "#800000", "darkyellow" => "#808000", "darkgray" => "#808080", "lightgray" => "#d3d3d3",
        _ => null
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.OpenXmlHighlightColor failed: {exception.Message}");
            throw;
        }
    }

    private bool IsVisibleCssBackground(string? value) {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.IsVisibleCssBackground.");
            return !string.IsNullOrWhiteSpace(value)
        && !string.Equals(value, "transparent", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(value, "none", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.IsVisibleCssBackground failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Normalizes CSS background.
    /// </summary>
    public string NormalizeCssBackground(string? value)
    {
        try
        {
            logger.LogTrace($"Delegating publication CSS background normalization.");
            return _markup.NormalizeCssBackground(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize a publication CSS background: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the sanitize preview HTML operation.
    /// </summary>
    public string SanitizePreviewHtml(string html)
    {
        try
        {
            logger.LogTrace($"Delegating publication preview sanitization.");
            return _markup.SanitizePreviewHtml(html);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not sanitize publication preview HTML: {exception.Message}");
            throw;
        }
    }

    private void NormalizeStreaming(PublicationDocument document)
    {
        try
        {
            logger.LogTrace($"Entering PublicationFileService.NormalizeStreaming.");
                    var streaming = document.Streaming;
                    streaming.Outputs ??= [];
                    streaming.Recording ??= new PublicationRecordingSettings();
                    streaming.Lan ??= new PublicationLanStreamingSettings();
                    streaming.Hotkeys ??= [];
                    streaming.MasterWidth = Math.Clamp(streaming.MasterWidth <= 0 ? 3840 : streaming.MasterWidth, 320, 7680);
                    streaming.MasterHeight = Math.Clamp(streaming.MasterHeight <= 0 ? 2160 : streaming.MasterHeight, 180, 4320);
                    streaming.MasterFrameRate = Math.Clamp(streaming.MasterFrameRate <= 0 ? 60 : streaming.MasterFrameRate, 15, 120);
                    if (streaming.ProgramPageId is { } pageId && document.Pages.All(page => page.Id != pageId)) streaming.ProgramPageId = null;
                    foreach (var output in streaming.Outputs)
                    {
                        output.Name = string.IsNullOrWhiteSpace(output.Name) ? output.Provider.ToString() : output.Name.Trim();
                        output.Width = Math.Clamp(output.Width <= 0 ? 1920 : output.Width, 320, 7680);
                        output.Height = Math.Clamp(output.Height <= 0 ? 1080 : output.Height, 180, 4320);
                        output.FrameRate = Math.Clamp(output.FrameRate <= 0 ? 60 : output.FrameRate, 15, 120);
                        output.VideoBitrateKbps = Math.Clamp(output.VideoBitrateKbps <= 0 ? 6000 : output.VideoBitrateKbps, 250, 200000);
                        output.AudioBitrateKbps = Math.Clamp(output.AudioBitrateKbps <= 0 ? 160 : output.AudioBitrateKbps, 32, 1024);
                        output.KeyFrameIntervalSeconds = Math.Clamp(output.KeyFrameIntervalSeconds <= 0 ? 2 : output.KeyFrameIntervalSeconds, 1, 10);
                    }
                    streaming.Recording.SelectedOutputIds ??= [];
                    streaming.Recording.SelectedOutputIds = streaming.Recording.SelectedOutputIds.Where(id => streaming.Outputs.Any(output => output.Id == id)).Distinct().ToList();
                    streaming.Recording.SegmentSeconds = Math.Clamp(streaming.Recording.SegmentSeconds <= 0 ? 10 : streaming.Recording.SegmentSeconds, 2, 120);
                    streaming.Lan.Port = Math.Clamp(streaming.Lan.Port <= 0 ? 17848 : streaming.Lan.Port, 1024, 65535);
                    streaming.Lan.Width = Math.Clamp(streaming.Lan.Width <= 0 ? 1920 : streaming.Lan.Width, 320, 7680);
                    streaming.Lan.Height = Math.Clamp(streaming.Lan.Height <= 0 ? 1080 : streaming.Lan.Height, 180, 4320);
                    streaming.Lan.FrameRate = Math.Clamp(streaming.Lan.FrameRate <= 0 ? 60 : streaming.Lan.FrameRate, 15, 120);
                    streaming.Lan.VideoBitrateKbps = Math.Clamp(streaming.Lan.VideoBitrateKbps <= 0 ? 8000 : streaming.Lan.VideoBitrateKbps, 250, 200000);
                    streaming.Lan.ViewerLimit = Math.Clamp(streaming.Lan.ViewerLimit <= 0 ? 50 : streaming.Lan.ViewerLimit, 1, 10000);
                    streaming.Hotkeys = streaming.Hotkeys
                        .Where(item => !string.IsNullOrWhiteSpace(item.Gesture) && !string.IsNullOrWhiteSpace(item.Command))
                        .GroupBy(item => item.Id == Guid.Empty ? Guid.NewGuid() : item.Id)
                        .Select(group => group.First())
                        .ToList();
                    foreach (var hotkey in streaming.Hotkeys)
                    {
                        if (hotkey.Id == Guid.Empty) hotkey.Id = Guid.NewGuid();
                        hotkey.Gesture = hotkey.Gesture.Trim();
                        hotkey.Command = hotkey.Command.Trim();
                        if (hotkey.Command == "SelectPage" && hotkey.TargetId is { } targetPage && document.Pages.All(page => page.Id != targetPage)) hotkey.TargetId = null;
                        if (hotkey.Command == "ToggleOutput" && hotkey.TargetId is { } targetOutput && streaming.Outputs.All(output => output.Id != targetOutput)) hotkey.TargetId = null;
                    }
                    foreach (var source in _elementTraversal.Descendants(document).OfType<LiveSourceElement>())
                    {
                        source.CaptureWidth = Math.Clamp(source.CaptureWidth <= 0 ? 1920 : source.CaptureWidth, 320, 7680);
                        source.CaptureHeight = Math.Clamp(source.CaptureHeight <= 0 ? 1080 : source.CaptureHeight, 180, 4320);
                        source.CaptureFrameRate = Math.Clamp(source.CaptureFrameRate <= 0 ? 60 : source.CaptureFrameRate, 15, 120);
                        source.Volume = Math.Clamp(source.Volume, 0, 1);
                        source.AudioDelayMilliseconds = Math.Clamp(source.AudioDelayMilliseconds, -10000, 10000);
                        source.Brightness = Math.Clamp(source.Brightness <= 0 ? 1 : source.Brightness, 0, 4);
                        source.Contrast = Math.Clamp(source.Contrast <= 0 ? 1 : source.Contrast, 0, 4);
                        source.Saturation = Math.Clamp(source.Saturation <= 0 ? 1 : source.Saturation, 0, 4);
                        source.HueRotation = Math.Clamp(source.HueRotation, -360, 360);
                        source.Blur = Math.Clamp(source.Blur, 0, 64);
                        source.ChromaSimilarity = Math.Clamp(source.ChromaSimilarity, 0, 1);
                        source.ChromaSmoothness = Math.Clamp(source.ChromaSmoothness, 0, 1);
                        source.ChromaSpill = Math.Clamp(source.ChromaSpill, 0, 1);
                        source.ChromaResidualOpacity = Math.Clamp(source.ChromaResidualOpacity, 0, 1);
                        source.VideoLayers ??= [];
                        _mediaTimeline.SynchronizeLiveSourceLayer(source);
                        source.Width = Math.Max(source.IsVisual ? 30 : 35, source.Width);
                        source.Height = Math.Max(source.IsVisual ? 20 : 12, source.Height);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationFileService.NormalizeStreaming failed: {exception.Message}");
            throw;
        }
    }

}
