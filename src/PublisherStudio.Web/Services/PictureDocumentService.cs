using System.Text.Json;
using System.Text.Json.Serialization;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Coordinates picture document behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class PictureDocumentService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Performs serialize as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Serialize(PictureDocument document) {
    try
    {
        return JsonSerializer.Serialize(document, _options);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.Serialize failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs deserialize as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The picture document produced by the operation.</returns>
    public PictureDocument Deserialize(string json)
    {
    try
    {
            var document = JsonSerializer.Deserialize<PictureDocument>(json, _options)
                ?? throw new InvalidDataException("The picture document is empty or invalid.");
            Normalize(document);
            return document;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.Deserialize failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs clone as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The picture document produced by the operation.</returns>
    public PictureDocument Clone(PictureDocument document) {
    try
    {
        return Deserialize(Serialize(document));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.Clone failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Adds raster layer as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="dataUrl">Data url value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="naturalWidth">Natural width value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="naturalHeight">Natural height value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="centerX">Center x value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="centerY">Center y value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The raster picture layer produced by the operation.</returns>
    public RasterPictureLayer AddRasterLayer(
        PictureDocument document,
        string dataUrl,
        string name,
        int naturalWidth = 0,
        int naturalHeight = 0,
        double? centerX = null,
        double? centerY = null)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(document);
            if (string.IsNullOrWhiteSpace(dataUrl))
                throw new InvalidDataException("The picture layer does not contain image data.");

            Normalize(document);
            var sourceWidth = naturalWidth > 0 ? naturalWidth : document.WidthPx;
            var sourceHeight = naturalHeight > 0 ? naturalHeight : document.HeightPx;
            var size = FitSize(sourceWidth, sourceHeight, document.WidthPx * .72, document.HeightPx * .72);
            var layer = new RasterPictureLayer
            {
                Name = NextLayerName(document, string.IsNullOrWhiteSpace(name) ? "Picture" : Path.GetFileNameWithoutExtension(name)),
                DataUrl = dataUrl,
                Width = size.Width,
                Height = size.Height,
                X = Math.Clamp((centerX ?? document.WidthPx / 2d) - size.Width / 2d, -size.Width + 1, document.WidthPx - 1),
                Y = Math.Clamp((centerY ?? document.HeightPx / 2d) - size.Height / 2d, -size.Height + 1, document.HeightPx - 1)
            };
            document.Layers.Add(layer);
            return layer;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.AddRasterLayer failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs normalize as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the picture document operation and used when producing its result.</param>
    public void Normalize(PictureDocument document)
    {
    try
    {
            document.FormatVersion = "1.4";
            document.WidthPx = Math.Clamp(document.WidthPx, 16, 8192);
            document.HeightPx = Math.Clamp(document.HeightPx, 16, 8192);
            document.Zoom = Math.Clamp(document.Zoom <= 0 ? .65 : document.Zoom, .05, 4);
            document.GridSpacingPx = Math.Clamp(document.GridSpacingPx <= 0 ? 25 : document.GridSpacingPx, 2, 1000);
            document.Background = string.IsNullOrWhiteSpace(document.Background) ? "transparent" : document.Background;
            document.Layers ??= [];

            foreach (var layer in document.Layers)
            {
                layer.Name = string.IsNullOrWhiteSpace(layer.Name) ? layer.Kind.ToString() : layer.Name;
                layer.GroupPath = layer.GroupPath?.Trim() ?? string.Empty;
                layer.Width = Math.Clamp(layer.Width <= 0 ? 1 : layer.Width, 1, 16384);
                layer.Height = Math.Clamp(layer.Height <= 0 ? 1 : layer.Height, 1, 16384);
                layer.Rotation = NormalizeAngle(layer.Rotation);
                layer.Opacity = Math.Clamp(layer.Opacity, 0, 1);
                layer.Brightness = Math.Clamp(layer.Brightness, 0, 3);
                layer.Contrast = Math.Clamp(layer.Contrast, 0, 3);
                layer.Saturation = Math.Clamp(layer.Saturation, 0, 3);
                layer.HueRotation = Math.Clamp(layer.HueRotation, -360, 360);
                layer.Blur = Math.Clamp(layer.Blur, 0, 100);
                layer.Grayscale = Math.Clamp(layer.Grayscale, 0, 1);
                layer.Sepia = Math.Clamp(layer.Sepia, 0, 1);
                layer.Invert = Math.Clamp(layer.Invert, 0, 1);
                layer.ClipPolygon ??= [];
                if (layer.ClipPolygon.Count > 2048) layer.ClipPolygon = layer.ClipPolygon.Take(2048).ToList();
                foreach (var point in layer.ClipPolygon)
                {
                    point.X = Math.Clamp(point.X, -16384, 32768);
                    point.Y = Math.Clamp(point.Y, -16384, 32768);
                }
                if (layer.ClipPolygon.Count is > 0 and < 3) layer.ClipPolygon.Clear();

                switch (layer)
                {
                    case RasterPictureLayer raster:
                        raster.TintOpacity = Math.Clamp(raster.TintOpacity, 0, 1);
                        break;
                    case TextPictureLayer text:
                        text.FontSizePx = Math.Clamp(text.FontSizePx <= 0 ? 48 : text.FontSizePx, 4, 1024);
                        text.OutlineWidthPx = Math.Clamp(text.OutlineWidthPx, 0, 64);
                        text.ShadowBlurPx = Math.Clamp(text.ShadowBlurPx, 0, 200);
                        break;
                    case ShapePictureLayer shape:
                        shape.StrokeWidthPx = Math.Clamp(shape.StrokeWidthPx, 0, 200);
                        shape.CornerRadiusPx = Math.Clamp(shape.CornerRadiusPx, 0, 2000);
                        shape.PathPoints ??= [];
                        if (shape.PathPoints.Count > 20000) shape.PathPoints = shape.PathPoints.Take(20000).ToList();
                        foreach (var point in shape.PathPoints)
                        {
                            point.X = Math.Clamp(point.X, -16384, 32768);
                            point.Y = Math.Clamp(point.Y, -16384, 32768);
                        }
                        if (shape.Shape == PictureShapeKind.Path && shape.PathPoints.Count < 2)
                            shape.Shape = PictureShapeKind.Freeform;
                        break;
                    case SvgPictureLayer vector:
                        vector.SvgMarkup = NormalizeSvgMarkup(vector.SvgMarkup);
                        vector.SourceFormat = string.IsNullOrWhiteSpace(vector.SourceFormat) ? "SVG" : vector.SourceFormat.Trim();
                        vector.SourceElementId = vector.SourceElementId?.Trim() ?? string.Empty;
                        break;
                    case RenderPictureLayer render:
                        render.Detail = Math.Clamp(render.Detail, 1, 8);
                        render.Scale = Math.Clamp(render.Scale <= 0 ? 90 : render.Scale, 4, 2000);
                        render.Softness = Math.Clamp(render.Softness, 0, 1);
                        render.RenderContrast = Math.Clamp(render.RenderContrast <= 0 ? 1 : render.RenderContrast, .1, 5);
                        render.StripeWidthPx = Math.Clamp(render.StripeWidthPx <= 0 ? 32 : render.StripeWidthPx, 1, 1000);
                        break;
                    case PaintPictureLayer paint:
                        paint.Strokes ??= [];
                        foreach (var stroke in paint.Strokes)
                        {
                            stroke.Color = string.IsNullOrWhiteSpace(stroke.Color) ? "#111827" : stroke.Color;
                            stroke.WidthPx = Math.Clamp(stroke.WidthPx <= 0 ? 1 : stroke.WidthPx, .25, 512);
                            stroke.Opacity = Math.Clamp(stroke.Opacity, 0, 1);
                            stroke.Hardness = Math.Clamp(stroke.Hardness, 0, 1);
                            stroke.Points ??= [];
                            if (stroke.Points.Count > 20000) stroke.Points = stroke.Points.Take(20000).ToList();
                            foreach (var point in stroke.Points)
                            {
                                point.X = Math.Clamp(point.X, -16384, 32768);
                                point.Y = Math.Clamp(point.Y, -16384, 32768);
                            }
                        }
                        break;
                }
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.Normalize failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes SVG markup as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="markup">Markup value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeSvgMarkup(string? markup)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(markup)) return string.Empty;
            var trimmed = markup.Trim();
            if (!trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A vector picture layer must contain a standalone SVG document.");
            if (trimmed.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A vector picture layer contains active SVG content.");
            return trimmed;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.NormalizeSvgMarkup failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs fit size as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="width">Width value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="maxWidth">Max width value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="maxHeight">Max height value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The double width double height produced by the operation.</returns>
    private (double Width, double Height) FitSize(double width, double height, double maxWidth, double maxHeight)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var scale = Math.Min(maxWidth / width, maxHeight / height);
        if (!double.IsFinite(scale) || scale <= 0) scale = 1;
        return (Math.Max(1, width * scale), Math.Max(1, height * scale));
    }

    /// <summary>
    /// Performs next layer name as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the picture document operation and used when producing its result.</param>
    /// <param name="requested">Requested value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NextLayerName(PictureDocument document, string requested)
    {
    try
    {
            requested = string.IsNullOrWhiteSpace(requested) ? "Layer" : requested.Trim();
            if (document.Layers.All(layer => !string.Equals(layer.Name, requested, StringComparison.OrdinalIgnoreCase)))
                return requested;
            for (var suffix = 2; suffix < 100_000; suffix++)
            {
                var candidate = $"{requested} {suffix}";
                if (document.Layers.All(layer => !string.Equals(layer.Name, candidate, StringComparison.OrdinalIgnoreCase)))
                    return candidate;
            }
            return $"{requested} {Guid.NewGuid():N}";
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.NextLayerName failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Normalizes angle as part of the picture document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the picture document operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double NormalizeAngle(double value) {
    try
    {
        return (value % 360 + 360) % 360;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PictureDocumentService.NormalizeAngle failed: {__serviceMethodException}");
        throw;
    }
}
}
