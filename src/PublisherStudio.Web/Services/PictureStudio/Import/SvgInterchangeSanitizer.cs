using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace PublisherStudio.Services.PictureStudio.Import;

/// <summary>
/// Represents a SVG interchange sanitizer application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SvgInterchangeSanitizer(ILogger<SvgInterchangeSanitizer> logger)
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly HashSet<string> RemovedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "foreignObject", "iframe", "object", "embed", "audio", "video", "canvas"
    };

    /// <summary>
    /// Performs sanitize for <see cref="SvgInterchangeSanitizer"/>, keeping the operation consistent with the state and invariants of the surrounding SVG interchange sanitizer workflow.
    /// </summary>
    /// <param name="svg">Svg value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Sanitize(string svg)
    {
        try
        {
            logger.LogTrace($"Entering SvgInterchangeSanitizer.Sanitize.");
                    if (string.IsNullOrWhiteSpace(svg))
                        throw new InvalidDataException("The SVG document is empty.");

                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                        MaxCharactersFromEntities = 0,
                        MaxCharactersInDocument = 0
                    };
                    using var stringReader = new StringReader(svg);
                    using var reader = XmlReader.Create(stringReader, settings);
                    var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
                    var root = document.Root;
                    if (root is null || !string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The selected file is not an SVG document.");

                    foreach (var element in root.DescendantsAndSelf().ToList())
                    {
                        if (RemovedElements.Contains(element.Name.LocalName))
                        {
                            element.Remove();
                            continue;
                        }

                        foreach (var attribute in element.Attributes().ToList())
                        {
                            var local = attribute.Name.LocalName;
                            var value = attribute.Value.Trim();
                            if (local.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                                value.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                                value.Contains("vbscript:", StringComparison.OrdinalIgnoreCase) ||
                                ContainsExternalCssReference(value) ||
                                IsExternalReference(local, value))
                            {
                                attribute.Remove();
                            }
                        }

                        if (string.Equals(element.Name.LocalName, "style", StringComparison.OrdinalIgnoreCase))
                        {
                            var css = element.Value;
                            if (css.Contains("@import", StringComparison.OrdinalIgnoreCase) ||
                                css.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                                css.Contains("url(http", StringComparison.OrdinalIgnoreCase) ||
                                css.Contains("url(//", StringComparison.OrdinalIgnoreCase))
                                element.Remove();
                        }
                    }

                    root.SetAttributeValue("xmlns", "http://www.w3.org/2000/svg");
                    return document.ToString(SaveOptions.DisableFormatting);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SvgInterchangeSanitizer.Sanitize failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads viewport for <see cref="SvgInterchangeSanitizer"/>, keeping the operation consistent with the state and invariants of the surrounding SVG interchange sanitizer workflow.
    /// </summary>
    /// <param name="svg">Svg value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <returns>The double width double height double min x double min y produced by the operation.</returns>
    public (double Width, double Height, double MinX, double MinY) ReadViewport(string svg)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var stringReader = new StringReader(svg);
        using var reader = XmlReader.Create(stringReader, settings);
        var document = XDocument.Load(reader);
        var root = document.Root ?? throw new InvalidDataException("The SVG document has no root element.");
        var viewBox = ParseNumbers(root.Attribute("viewBox")?.Value, 4);
        var minX = viewBox.Length == 4 ? viewBox[0] : 0;
        var minY = viewBox.Length == 4 ? viewBox[1] : 0;
        var width = ParseLength(root.Attribute("width")?.Value, viewBox.Length == 4 ? viewBox[2] : 0);
        var height = ParseLength(root.Attribute("height")?.Value, viewBox.Length == 4 ? viewBox[3] : 0);
        if (width <= 0 && viewBox.Length == 4) width = viewBox[2];
        if (height <= 0 && viewBox.Length == 4) height = viewBox[3];
        if (width <= 0) width = 1200;
        if (height <= 0) height = 800;
        return (width, height, minX, minY);
    }

    /// <summary>
    /// Parses length for <see cref="SvgInterchangeSanitizer"/>, keeping the operation consistent with the state and invariants of the surrounding SVG interchange sanitizer workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    public double ParseLength(string? value, double fallback = 0)
    {
        try
        {
            logger.LogTrace($"Entering SvgInterchangeSanitizer.ParseLength.");
                    if (string.IsNullOrWhiteSpace(value)) return fallback;
                    var text = value.Trim();
                    var numberLength = 0;
                    while (numberLength < text.Length && (char.IsDigit(text[numberLength]) || text[numberLength] is '.' or '-' or '+' or 'e' or 'E'))
                        numberLength++;
                    if (!double.TryParse(text[..numberLength], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                        return fallback;
                    var unit = text[numberLength..].Trim().ToLowerInvariant();
                    return unit switch
                    {
                        "mm" => number * 96d / 25.4d,
                        "cm" => number * 96d / 2.54d,
                        "in" => number * 96d,
                        "pt" => number * 96d / 72d,
                        "pc" => number * 16d,
                        "q" => number * 96d / 101.6d,
                        "%" => fallback,
                        _ => number
                    };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SvgInterchangeSanitizer.ParseLength failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs contains external CSS reference for <see cref="SvgInterchangeSanitizer"/>, keeping the operation consistent with the state and invariants of the surrounding SVG interchange sanitizer workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool ContainsExternalCssReference(string value)
    {
        try
        {
            logger.LogTrace($"Entering SvgInterchangeSanitizer.ContainsExternalCssReference.");
                    if (string.IsNullOrWhiteSpace(value)) return false;
                    if (value.Contains("@import", StringComparison.OrdinalIgnoreCase)) return true;

                    var searchFrom = 0;
                    while (searchFrom < value.Length)
                    {
                        var start = value.IndexOf("url(", searchFrom, StringComparison.OrdinalIgnoreCase);
                        if (start < 0) return false;
                        var end = value.IndexOf(')', start + 4);
                        if (end < 0) return true;
                        var target = value[(start + 4)..end].Trim().Trim('\"', '\'');
                        if (!target.StartsWith('#') && !IsSafeEmbeddedRaster(target)) return true;
                        searchFrom = end + 1;
                    }
                    return false;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SvgInterchangeSanitizer.ContainsExternalCssReference failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether external reference for <see cref="SvgInterchangeSanitizer"/>, keeping the operation consistent with the state and invariants of the surrounding SVG interchange sanitizer workflow.
    /// </summary>
    /// <param name="localName">Local name value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsExternalReference(string localName, string value)
    {
        try
        {
            logger.LogTrace($"Entering SvgInterchangeSanitizer.IsExternalReference.");
                    if (!localName.Equals("href", StringComparison.OrdinalIgnoreCase) &&
                        !localName.Equals("src", StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (string.IsNullOrWhiteSpace(value) || value.StartsWith('#')) return false;
                    return !IsSafeEmbeddedRaster(value);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SvgInterchangeSanitizer.IsExternalReference failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether safe embedded raster for <see cref="SvgInterchangeSanitizer"/>, keeping the operation consistent with the state and invariants of the surrounding SVG interchange sanitizer workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSafeEmbeddedRaster(string value)
    {
        try
        {
            logger.LogTrace($"Entering SvgInterchangeSanitizer.IsSafeEmbeddedRaster.");
                    var normalized = value.Trim();
                    return normalized.StartsWith("data:image/png", StringComparison.OrdinalIgnoreCase) ||
                           normalized.StartsWith("data:image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                           normalized.StartsWith("data:image/jpg", StringComparison.OrdinalIgnoreCase) ||
                           normalized.StartsWith("data:image/gif", StringComparison.OrdinalIgnoreCase) ||
                           normalized.StartsWith("data:image/webp", StringComparison.OrdinalIgnoreCase) ||
                           normalized.StartsWith("data:image/bmp", StringComparison.OrdinalIgnoreCase);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SvgInterchangeSanitizer.IsSafeEmbeddedRaster failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses numbers for <see cref="SvgInterchangeSanitizer"/>, keeping the operation consistent with the state and invariants of the surrounding SVG interchange sanitizer workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the SVG interchange sanitizer operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double[] ParseNumbers(string? value, int maximum)
    {
        try
        {
            logger.LogTrace($"Entering SvgInterchangeSanitizer.ParseNumbers.");
                    if (string.IsNullOrWhiteSpace(value)) return [];
                    return value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
                        .Select(item => double.TryParse(item, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : double.NaN)
                        .Where(double.IsFinite)
                        .Take(maximum)
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SvgInterchangeSanitizer.ParseNumbers failed: {exception.Message}");
            throw;
        }
    }
}
