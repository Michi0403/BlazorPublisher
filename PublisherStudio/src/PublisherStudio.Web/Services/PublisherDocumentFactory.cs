using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

public interface IPublisherDocumentFactory
{
    PublicationDocument CreatePublication();
    PublicationPage CreatePage(string? name = null);
    PictureDocument CreatePicture(int? widthPixels = null, int? heightPixels = null, bool transparent = true);
    PictureDocument CreatePictureFromRaster(string dataUrl, string name, int? widthPixels = null, int? heightPixels = null);
}

public sealed class PublisherDocumentFactory(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<PublisherDocumentFactory> logger) : IPublisherDocumentFactory
{
    public PublicationDocument CreatePublication()
    {
        try
        {
            logger.LogTrace($"Creating the default publication document from the runtime policy.");
            var defaults = runtimePolicy.DocumentDefaults;
            var document = new PublicationDocument
            {
                Name = defaults.PublicationName,
                FormatVersion = defaults.PublicationFormatVersion,
                Zoom = defaults.PublicationZoom
            };
            var page = CreatePage();
            page.Elements.Add(new TextFrameElement
            {
                Name = defaults.TitleName,
                X = defaults.TitleX,
                Y = defaults.TitleY,
                Width = defaults.TitleWidth,
                Height = defaults.TitleHeight,
                PreviewHtml = defaults.TitlePreviewHtml,
                DocumentContent = [],
                StoryFormat = StoryStorageFormat.OpenXml,
                ZIndex = 2
            });
            page.Elements.Add(new ShapeElement
            {
                Name = defaults.AccentName,
                Shape = PublicationShape.Rectangle,
                X = defaults.AccentX,
                Y = defaults.AccentY,
                Width = defaults.AccentWidth,
                Height = defaults.AccentHeight,
                Fill = defaults.AccentFill,
                Stroke = defaults.AccentStroke,
                ZIndex = 1
            });
            document.Pages.Add(page);
            return document;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create the default publication document: {exception.Message}");
            throw;
        }
    }

    public PublicationPage CreatePage(string? name = null)
    {
        try
        {
            var defaults = runtimePolicy.DocumentDefaults;
            logger.LogTrace($"Creating publication page {name ?? defaults.PageName} from the runtime policy.");
            return new PublicationPage
            {
                Name = string.IsNullOrWhiteSpace(name) ? defaults.PageName : name,
                WidthMm = defaults.PageWidthMillimeters,
                HeightMm = defaults.PageHeightMillimeters,
                Background = defaults.PageBackground
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create a publication page: {exception.Message}");
            throw;
        }
    }

    public PictureDocument CreatePicture(int? widthPixels = null, int? heightPixels = null, bool transparent = true)
    {
        try
        {
            var defaults = runtimePolicy.DocumentDefaults;
            var width = Math.Clamp(widthPixels ?? defaults.PictureWidthPixels, defaults.PictureMinimumDimension, defaults.PictureMaximumDimension);
            var height = Math.Clamp(heightPixels ?? defaults.PictureHeightPixels, defaults.PictureMinimumDimension, defaults.PictureMaximumDimension);
            logger.LogTrace($"Creating picture document {width}x{height} from the runtime policy.");
            return new PictureDocument
            {
                Name = defaults.PictureName,
                FormatVersion = defaults.PictureFormatVersion,
                WidthPx = width,
                HeightPx = height,
                Background = transparent ? defaults.PictureTransparentBackground : defaults.PictureOpaqueBackground,
                Zoom = defaults.PictureZoom,
                GridSpacingPx = defaults.PictureGridSpacingPixels
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create a picture document: {exception.Message}");
            throw;
        }
    }

    public PictureDocument CreatePictureFromRaster(string dataUrl, string name, int? widthPixels = null, int? heightPixels = null)
    {
        try
        {
            var document = CreatePicture(widthPixels, heightPixels, true);
            document.Name = string.IsNullOrWhiteSpace(name) ? runtimePolicy.DocumentDefaults.PictureName : Path.GetFileNameWithoutExtension(name);
            document.Layers.Add(new RasterPictureLayer
            {
                Name = string.IsNullOrWhiteSpace(name) ? runtimePolicy.DocumentDefaults.PictureName : name,
                DataUrl = dataUrl,
                X = 0,
                Y = 0,
                Width = document.WidthPx,
                Height = document.HeightPx,
                FitMode = PictureRasterFitMode.Contain
            });
            logger.LogTrace($"Created raster-backed picture document {document.Name}.");
            return document;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create a raster-backed picture document: {exception.Message}");
            throw;
        }
    }
}

public interface IPublicationGridRowFactory
{
    PublicationGridRow Create(PublicationDataRow row, IReadOnlyList<string> columns);
}

public sealed class PublicationGridRowFactory(ILogger<PublicationGridRowFactory> logger) : IPublicationGridRowFactory
{
    public PublicationGridRow Create(PublicationDataRow row, IReadOnlyList<string> columns)
    {
        try
        {
            logger.LogTrace($"Creating a publication grid row from {columns.Count} configured columns.");
            var values = columns.Take(8).Select(row.Get).Concat(Enumerable.Repeat(string.Empty, 8)).Take(8).ToArray();
            return new PublicationGridRow
            {
                C1 = values[0], C2 = values[1], C3 = values[2], C4 = values[3],
                C5 = values[4], C6 = values[5], C7 = values[6], C8 = values[7]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create a publication grid row: {exception.Message}");
            throw;
        }
    }
}
