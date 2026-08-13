using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.PictureStudio.Import;

/// <summary>
/// Coordinates open raster import behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="documentFactory">Publisher document factory dependency used by the open raster import workflow to provide the corresponding application capability.</param>
/// <param name="svgSanitizer">Svg sanitizer value supplied to the open raster import operation and used when producing its result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OpenRasterImportService(IPublisherDocumentFactory documentFactory, 
    SvgInterchangeSanitizer svgSanitizer,
    ILogger<OpenRasterImportService> logger)
{
    /// <summary>
    /// Stores the internal layer name state used by <see cref="OpenRasterImportService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly XName LayerName = "layer";
    /// <summary>
    /// Stores the internal stack name state used by <see cref="OpenRasterImportService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly XName StackName = "stack";

    /// <summary>
    /// Performs import as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="input">Input value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="fileName">File name value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The picture import result produced by the operation.</returns>
    public async Task<PictureImportResult> ImportAsync(Stream input, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.ImportAsync.");
                    using var buffer = new MemoryStream();
                    await input.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                    buffer.Position = 0;
                    using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
                    var issues = new List<InterchangeIssue>();

                    var mimetype = archive.GetEntry("mimetype");
                    if (mimetype is not null)
                    {
                        await using var mimeStream = mimetype.Open();
                        using var mimeReader = new StreamReader(mimeStream, Encoding.ASCII, false, leaveOpen: false);
                        var value = (await mimeReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).Trim();
                        if (!string.Equals(value, "image/openraster", StringComparison.Ordinal))
                            throw new InvalidDataException("The archive is not an OpenRaster document.");
                    }
                    else
                    {
                        issues.Add(new(InterchangeIssueSeverity.Warning, "ORA_MIMETYPE_MISSING", "The OpenRaster mimetype entry is missing; stack.xml was used for compatibility."));
                    }

                    var stackEntry = archive.GetEntry("stack.xml")
                        ?? throw new InvalidDataException("The OpenRaster archive does not contain stack.xml.");
                    XDocument stackDocument;
                    await using (var stackStream = stackEntry.Open())
                    {
                        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersFromEntities = 0, MaxCharactersInDocument = 0 };
                        using var reader = XmlReader.Create(stackStream, settings);
                        stackDocument = XDocument.Load(reader);
                    }

                    var image = stackDocument.Root ?? throw new InvalidDataException("The OpenRaster stack is empty.");
                    var sourceWidth = ReadInt(image.Attribute("w")?.Value, 1200);
                    var sourceHeight = ReadInt(image.Attribute("h")?.Value, 800);
                    var scale = Math.Min(1d, Math.Min(8192d / Math.Max(1, sourceWidth), 8192d / Math.Max(1, sourceHeight)));
                    if (scale < 1)
                        issues.Add(new(InterchangeIssueSeverity.Warning, "ORA_CANVAS_SCALED", $"The {sourceWidth} × {sourceHeight} canvas was proportionally reduced to fit Picture Studio's 8192 px limit."));

                    var document = documentFactory.CreatePicture(
                        Math.Max(16, (int)Math.Round(sourceWidth * scale)),
                        Math.Max(16, (int)Math.Round(sourceHeight * scale)),
                        true);
                    document.Name = string.IsNullOrWhiteSpace(fileName) ? "OpenRaster" : Path.GetFileNameWithoutExtension(fileName);
                    document.FormatVersion = "1.4";
                    document.GridVisible = false;

                    var flattened = new List<PictureLayer>();
                    var rootStack = image.Elements().FirstOrDefault(element => element.Name.LocalName == StackName.LocalName)
                        ?? throw new InvalidDataException("The OpenRaster document does not contain a root layer stack.");
                    await ReadStackAsync(archive, rootStack, flattened, issues, string.Empty, 1, true, scale, cancellationToken).ConfigureAwait(false);

                    // OpenRaster lists the uppermost layer first. Picture Studio renders bottom to top.
                    flattened.Reverse();
                    document.Layers.AddRange(flattened);
                    return new PictureImportResult { Document = document, Issues = issues };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.ImportAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads stack as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="archive">Archive value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="stack">Stack value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="layers">Layers value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="issues">Issues value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="parentPath">Parent path value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="parentOpacity">Parent opacity value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="parentVisible">Value indicating whether parent visible should apply to this operation.</param>
    /// <param name="scale">Scale value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ReadStackAsync(
        ZipArchive archive,
        XElement stack,
        List<PictureLayer> layers,
        List<InterchangeIssue> issues,
        string parentPath,
        double parentOpacity,
        bool parentVisible,
        double scale,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.ReadStackAsync.");
                    var stackName = CleanName(stack.Attribute("name")?.Value, string.Empty);
                    var groupPath = string.IsNullOrWhiteSpace(stackName)
                        ? parentPath
                        : string.IsNullOrWhiteSpace(parentPath) ? stackName : $"{parentPath} / {stackName}";
                    var opacity = parentOpacity * ReadDouble(stack.Attribute("opacity")?.Value, 1);
                    var visible = parentVisible && !string.Equals(stack.Attribute("visibility")?.Value, "hidden", StringComparison.OrdinalIgnoreCase);
                    var isolation = stack.Attribute("isolation")?.Value;
                    if (!string.IsNullOrWhiteSpace(stackName) &&
                        (!Nearly(opacity, parentOpacity) || string.Equals(isolation, "auto", StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new(InterchangeIssueSeverity.Warning, "ORA_GROUP_FLATTENED", $"Layer group '{groupPath}' was mapped to flat Picture Studio layers; group compositing is approximated.", groupPath));

                    foreach (var child in stack.Elements())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (child.Name.LocalName == StackName.LocalName)
                        {
                            await ReadStackAsync(archive, child, layers, issues, groupPath, opacity, visible, scale, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                        if (child.Name.LocalName != LayerName.LocalName) continue;

                        var source = child.Attribute("src")?.Value?.Replace('\\', '/').Trim();
                        if (string.IsNullOrWhiteSpace(source) || source.StartsWith('/') || source.Split('/').Any(part => part == ".."))
                        {
                            issues.Add(new(InterchangeIssueSeverity.Loss, "ORA_LAYER_SOURCE_INVALID", "A layer with an unsafe or missing source path was skipped.", groupPath));
                            continue;
                        }
                        var entry = archive.GetEntry(source);
                        if (entry is null)
                        {
                            issues.Add(new(InterchangeIssueSeverity.Loss, "ORA_LAYER_MISSING", $"Layer source '{source}' is missing and was skipped.", groupPath));
                            continue;
                        }

                        var name = CleanName(child.Attribute("name")?.Value, Path.GetFileNameWithoutExtension(source));
                        if (entry.Length <= 0)
                        {
                            issues.Add(new(InterchangeIssueSeverity.Loss, "ORA_LAYER_EMPTY", $"Layer '{name}' is empty and was skipped.", groupPath));
                            continue;
                        }
                        await using var layerStream = entry.Open();
                        using var layerBuffer = new MemoryStream();
                        await layerStream.CopyToAsync(layerBuffer, cancellationToken).ConfigureAwait(false);
                        var bytes = layerBuffer.ToArray();
                        var extension = Path.GetExtension(source).ToLowerInvariant();
                        var x = ReadInt(child.Attribute("x")?.Value, 0) * scale;
                        var y = ReadInt(child.Attribute("y")?.Value, 0) * scale;
                        var layerOpacity = opacity * ReadDouble(child.Attribute("opacity")?.Value, 1);
                        var layerVisible = visible && !string.Equals(child.Attribute("visibility")?.Value, "hidden", StringComparison.OrdinalIgnoreCase);
                        var blend = MapBlendMode(child.Attribute("composite-op")?.Value);
                        var locked = IsTrue(child.Attribute("edit-locked")?.Value) || IsTrue(child.Attribute("locked")?.Value);

                        if (extension is ".svg" or ".svgz")
                        {
                            try
                            {
                                var svgText = extension == ".svgz" ? DecompressSvg(bytes) : DecodeSvg(bytes);
                                var sanitized = svgSanitizer.Sanitize(svgText);
                                var (viewportWidth, viewportHeight, _, _) = svgSanitizer.ReadViewport(sanitized);
                                layers.Add(new SvgPictureLayer
                                {
                                    Name = name,
                                    GroupPath = groupPath,
                                    SvgMarkup = sanitized,
                                    SourceFormat = "OpenRaster SVG",
                                    SourceElementId = source,
                                    X = x,
                                    Y = y,
                                    Width = Math.Max(1, viewportWidth * scale),
                                    Height = Math.Max(1, viewportHeight * scale),
                                    Opacity = Math.Clamp(layerOpacity, 0, 1),
                                    Visible = layerVisible,
                                    Locked = locked,
                                    BlendMode = blend
                                });
                            }
                            catch (Exception ex) when (ex is InvalidDataException or XmlException or DecoderFallbackException)
                            {
                                issues.Add(new(InterchangeIssueSeverity.Loss, "ORA_SVG_LAYER_INVALID", $"Vector layer '{name}' could not be decoded safely and was skipped: {ex.Message}", groupPath));
                            }
                            continue;
                        }

                        var mime = extension switch
                        {
                            ".png" => "image/png",
                            ".jpg" or ".jpeg" => "image/jpeg",
                            ".webp" => "image/webp",
                            _ => string.Empty
                        };
                        if (string.IsNullOrWhiteSpace(mime))
                        {
                            issues.Add(new(InterchangeIssueSeverity.Loss, "ORA_LAYER_FORMAT_UNSUPPORTED", $"Layer '{name}' uses unsupported source format '{extension}' and was skipped.", groupPath));
                            continue;
                        }

                        var (imageWidth, imageHeight) = ReadImageSize(bytes, extension);
                        layers.Add(new RasterPictureLayer
                        {
                            Name = name,
                            GroupPath = groupPath,
                            DataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}",
                            X = x,
                            Y = y,
                            Width = Math.Max(1, (imageWidth > 0 ? imageWidth : 1) * scale),
                            Height = Math.Max(1, (imageHeight > 0 ? imageHeight : 1) * scale),
                            Opacity = Math.Clamp(layerOpacity, 0, 1),
                            Visible = layerVisible,
                            Locked = locked,
                            BlendMode = blend,
                            FitMode = PictureRasterFitMode.Stretch
                        });
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.ReadStackAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads image size as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="extension">Extension value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The int width int height produced by the operation.</returns>
    private (int Width, int Height) ReadImageSize(byte[] bytes, string extension)
    {
        if (extension == ".png" && bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            return (ReadBigEndianInt(bytes, 16), ReadBigEndianInt(bytes, 20));
        }
        return (0, 0);
    }


    /// <summary>
    /// Reads int as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ReadInt(string? value, int fallback) {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.ReadInt.");
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.ReadInt failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads double as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double ReadDouble(string? value, double fallback) {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.ReadDouble.");
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed)
            ? parsed
            : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.ReadDouble failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs map blend mode as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The picture blend mode produced by the operation.</returns>
    private PictureBlendMode MapBlendMode(string? value)
    {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.MapBlendMode.");
                    var operation = value?.Trim();
                    if (string.IsNullOrWhiteSpace(operation)) return PictureBlendMode.Normal;
                    var separator = operation.LastIndexOf(':');
                    if (separator >= 0 && separator < operation.Length - 1) operation = operation[(separator + 1)..];
                    return operation.ToLowerInvariant() switch
                    {
                        "multiply" => PictureBlendMode.Multiply,
                        "screen" => PictureBlendMode.Screen,
                        "overlay" => PictureBlendMode.Overlay,
                        "darken" => PictureBlendMode.Darken,
                        "lighten" => PictureBlendMode.Lighten,
                        _ => PictureBlendMode.Normal
                    };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.MapBlendMode failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs decode SVG as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DecodeSvg(byte[] bytes)
    {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.DecodeSvg.");
                    using var input = new MemoryStream(bytes, writable: false);
                    using var reader = new StreamReader(input, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
                    return reader.ReadToEnd();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.DecodeSvg failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs decompress SVG as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DecompressSvg(byte[] bytes)
    {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.DecompressSvg.");
                    try
                    {
                        using var input = new MemoryStream(bytes, writable: false);
                        using var gzip = new GZipStream(input, CompressionMode.Decompress, leaveOpen: false);
                        using var output = new MemoryStream();
                        gzip.CopyTo(output);
                        return DecodeSvg(output.ToArray());
                    }
                    catch (InvalidDataException)
                    {
                        throw;
                    }
                    catch (IOException exception)
                    {
                        throw new InvalidDataException("The SVGZ layer could not be decompressed.", exception);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.DecompressSvg failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads big endian int as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="offset">Offset value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ReadBigEndianInt(byte[] bytes, int offset)
    {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.ReadBigEndianInt.");
                    if (offset < 0 || offset > bytes.Length - 4) return 0;
                    var value = ((uint)bytes[offset] << 24)
                        | ((uint)bytes[offset + 1] << 16)
                        | ((uint)bytes[offset + 2] << 8)
                        | bytes[offset + 3];
                    return value <= int.MaxValue ? (int)value : 0;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.ReadBigEndianInt failed: {exception.Message}");
            throw;
        }
    }


    /// <summary>
    /// Performs clean name as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CleanName(string? value, string fallback) {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.CleanName.");
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.CleanName failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Determines whether true as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsTrue(string? value) {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.IsTrue.");
            return value is not null && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.IsTrue failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs nearly as part of the open raster import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="left">Left value supplied to the open raster import operation and used when producing its result.</param>
    /// <param name="right">Right value supplied to the open raster import operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool Nearly(double left, double right) {
        try
        {
            logger.LogTrace($"Entering OpenRasterImportService.Nearly.");
            return Math.Abs(left - right) < .0001;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"OpenRasterImportService.Nearly failed: {exception.Message}");
            throw;
        }
    }
}
