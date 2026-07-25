using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.PictureStudio.Import;

public sealed class OpenRasterImportService
{
    private static readonly XName LayerName = "layer";
    private static readonly XName StackName = "stack";

    public async Task<PictureImportResult> ImportAsync(Stream input, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
        var issues = new List<InterchangeIssue>();

        var mimetype = archive.GetEntry("mimetype");
        if (mimetype is not null)
        {
            await using var mimeStream = mimetype.Open();
            using var mimeReader = new StreamReader(mimeStream, Encoding.ASCII, false, leaveOpen: false);
            var value = (await mimeReader.ReadToEndAsync(cancellationToken)).Trim();
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

        var document = PictureDocument.CreateDefault(
            Math.Max(16, (int)Math.Round(sourceWidth * scale)),
            Math.Max(16, (int)Math.Round(sourceHeight * scale)),
            true);
        document.Name = string.IsNullOrWhiteSpace(fileName) ? "OpenRaster" : Path.GetFileNameWithoutExtension(fileName);
        document.FormatVersion = "1.4";
        document.GridVisible = false;

        var flattened = new List<PictureLayer>();
        var rootStack = image.Elements().FirstOrDefault(element => element.Name.LocalName == StackName.LocalName)
            ?? throw new InvalidDataException("The OpenRaster document does not contain a root layer stack.");
        await ReadStackAsync(archive, rootStack, flattened, issues, string.Empty, 1, true, scale, cancellationToken);

        // OpenRaster lists the uppermost layer first. Picture Studio renders bottom to top.
        flattened.Reverse();
        document.Layers.AddRange(flattened);
        return new PictureImportResult { Document = document, Issues = issues };
    }

    private static async Task ReadStackAsync(
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
                await ReadStackAsync(archive, child, layers, issues, groupPath, opacity, visible, scale, cancellationToken);
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
            await layerStream.CopyToAsync(layerBuffer, cancellationToken);
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
                    var sanitized = SvgInterchangeSanitizer.Sanitize(svgText);
                    var viewport = SvgInterchangeSanitizer.ReadViewport(sanitized);
                    layers.Add(new SvgPictureLayer
                    {
                        Name = name,
                        GroupPath = groupPath,
                        SvgMarkup = sanitized,
                        SourceFormat = "OpenRaster SVG",
                        SourceElementId = source,
                        X = x,
                        Y = y,
                        Width = Math.Max(1, viewport.Width * scale),
                        Height = Math.Max(1, viewport.Height * scale),
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

            var size = ReadImageSize(bytes, extension);
            layers.Add(new RasterPictureLayer
            {
                Name = name,
                GroupPath = groupPath,
                DataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}",
                X = x,
                Y = y,
                Width = Math.Max(1, (size.Width > 0 ? size.Width : 1) * scale),
                Height = Math.Max(1, (size.Height > 0 ? size.Height : 1) * scale),
                Opacity = Math.Clamp(layerOpacity, 0, 1),
                Visible = layerVisible,
                Locked = locked,
                BlendMode = blend,
                FitMode = PictureRasterFitMode.Stretch
            });
        }
    }

    private static (int Width, int Height) ReadImageSize(byte[] bytes, string extension)
    {
        if (extension == ".png" && bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            return (ReadBigEndianInt(bytes, 16), ReadBigEndianInt(bytes, 20));
        }
        return (0, 0);
    }


    private static string CleanName(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static bool IsTrue(string? value) => value is not null && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
    private static bool Nearly(double left, double right) => Math.Abs(left - right) < .0001;
}
