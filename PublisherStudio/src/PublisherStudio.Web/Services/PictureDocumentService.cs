using System.Text.Json;
using System.Text.Json.Serialization;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class PictureDocumentService
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize(PictureDocument document) => JsonSerializer.Serialize(document, _options);

    public PictureDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<PictureDocument>(json, _options)
            ?? throw new InvalidDataException("The picture document is empty or invalid.");
        Normalize(document);
        return document;
    }

    public PictureDocument Clone(PictureDocument document) => Deserialize(Serialize(document));

    public void Normalize(PictureDocument document)
    {
        document.FormatVersion = "1.1";
        document.WidthPx = Math.Clamp(document.WidthPx, 16, 8192);
        document.HeightPx = Math.Clamp(document.HeightPx, 16, 8192);
        document.Zoom = Math.Clamp(document.Zoom <= 0 ? .65 : document.Zoom, .05, 4);
        document.GridSpacingPx = Math.Clamp(document.GridSpacingPx <= 0 ? 25 : document.GridSpacingPx, 2, 1000);
        document.Background = string.IsNullOrWhiteSpace(document.Background) ? "transparent" : document.Background;
        document.Layers ??= [];

        foreach (var layer in document.Layers)
        {
            layer.Name = string.IsNullOrWhiteSpace(layer.Name) ? layer.Kind.ToString() : layer.Name;
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

    private static double NormalizeAngle(double value) => (value % 360 + 360) % 360;
}
