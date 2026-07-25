using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PublisherStudio.Domain;
using PublisherStudio.Services.PictureStudio.Import;

namespace PublisherStudio.Services.Publication.Import;

/// <summary>
/// Imports the open ODF drawing/presentation page model into PublisherStudio's canonical page system.
/// The adapter intentionally uses only BCL ZIP/XML APIs and reports every unsupported construct instead
/// of silently changing the native publication model to resemble ODF.
/// </summary>
public sealed partial class OpenDocumentImportService
{
    private static readonly XNamespace Office = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
    private static readonly XNamespace Draw = "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0";
    private static readonly XNamespace Style = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
    private static readonly XNamespace Text = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    private static readonly XNamespace Svg = "urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0";
    private static readonly XNamespace XLink = "http://www.w3.org/1999/xlink";
    private static readonly XNamespace Presentation = "urn:oasis:names:tc:opendocument:xmlns:presentation:1.0";

    public async Task<PublicationImportResult> ImportAsync(Stream source, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".fodg" or ".fodp"
            ? await ImportFlatAsync(source, fileName, cancellationToken)
            : await ImportPackageAsync(source, fileName, cancellationToken);
    }

    private async Task<PublicationImportResult> ImportPackageAsync(Stream source, string fileName, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await CopyWithLimitAsync(source, buffer, 512L * 1024 * 1024, cancellationToken);
        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
        var contentEntry = archive.GetEntry("content.xml") ?? throw new InvalidDataException("The OpenDocument package does not contain content.xml.");
        if (contentEntry.Length > 128L * 1024 * 1024) throw new InvalidDataException("The OpenDocument content.xml is too large.");

        XDocument content;
        await using (var contentStream = contentEntry.Open())
            content = await LoadXmlAsync(contentStream, cancellationToken);

        XDocument? styles = null;
        if (archive.GetEntry("styles.xml") is { } stylesEntry)
        {
            await using var styleStream = stylesEntry.Open();
            styles = await LoadXmlAsync(styleStream, cancellationToken);
        }

        return ImportDocuments(content, styles, fileName, path => ReadArchiveEntry(archive, path));
    }

    private async Task<PublicationImportResult> ImportFlatAsync(Stream source, string fileName, CancellationToken cancellationToken)
    {
        var content = await LoadXmlAsync(source, cancellationToken);
        return ImportDocuments(content, null, fileName, _ => null);
    }

    private static PublicationImportResult ImportDocuments(
        XDocument content,
        XDocument? styles,
        string fileName,
        Func<string, byte[]?> readAsset)
    {
        var issues = new List<InterchangeIssue>();
        var document = new PublicationDocument
        {
            Name = Path.GetFileNameWithoutExtension(fileName),
            FormatVersion = "1.53",
            Zoom = .8,
            View = new PublicationViewSettings(),
            Playback = new PublicationPlaybackSettings(),
            Streaming = new PublicationStreamingSettings()
        };

        var styleCatalog = BuildStyleCatalog(content, styles);
        var pageNodes = content.Descendants(Draw + "page").ToList();
        if (pageNodes.Count == 0)
            throw new InvalidDataException("The selected OpenDocument file does not contain drawing or presentation pages.");

        for (var pageIndex = 0; pageIndex < pageNodes.Count; pageIndex++)
        {
            var pageNode = pageNodes[pageIndex];
            var pageSize = ResolvePageSize(pageNode, styleCatalog);
            var page = new PublicationPage
            {
                Name = CleanName((string?)pageNode.Attribute(Draw + "name"), $"Page {pageIndex + 1}"),
                WidthMm = pageSize.Width,
                HeightMm = pageSize.Height,
                Background = ResolvePageBackground(pageNode, styleCatalog),
                Elements = [],
                Guides = [],
                Transition = new PublicationPageTransition()
            };
            var zIndex = 0;
            foreach (var child in pageNode.Elements())
                ImportElement(child, page, styleCatalog, readAsset, issues, ref zIndex, string.Empty);
            document.Pages.Add(page);
        }

        if (document.Pages.Count == 0) document.Pages.Add(PublicationPage.CreateA4());
        issues.Add(new(InterchangeIssueSeverity.Information, "ODF_PAGE_IMPORT", $"Imported {document.Pages.Count} OpenDocument page(s) into PublisherStudio's native page model.", fileName));
        return new PublicationImportResult { Document = document, Issues = issues };
    }

    private static void ImportElement(
        XElement element,
        PublicationPage page,
        StyleCatalog styles,
        Func<string, byte[]?> readAsset,
        List<InterchangeIssue> issues,
        ref int zIndex,
        string groupPath)
    {
        var local = element.Name.LocalName;
        var currentGroup = groupPath;
        if (element.Name == Draw + "g")
        {
            var groupName = CleanName((string?)element.Attribute(Draw + "name"), "Group");
            currentGroup = string.IsNullOrWhiteSpace(groupPath) ? groupName : $"{groupPath} / {groupName}";
            if (!string.IsNullOrWhiteSpace((string?)element.Attribute(Draw + "transform")))
                issues.Add(new(InterchangeIssueSeverity.Warning, "ODF_GROUP_TRANSFORM_APPROXIMATED", $"Group transform on '{currentGroup}' cannot be retained as a separate PublisherStudio group transform; child objects were imported in their stored coordinates and may need adjustment.", currentGroup));
            foreach (var child in element.Elements())
                ImportElement(child, page, styles, readAsset, issues, ref zIndex, currentGroup);
            return;
        }

        var bounds = ReadBounds(element, page);
        var style = styles.Resolve((string?)element.Attribute(Draw + "style-name") ?? (string?)element.Attribute(Presentation + "style-name"));
        var name = CleanName((string?)element.Attribute(Draw + "name"), $"{local} {zIndex + 1}");
        var visible = !string.Equals((string?)element.Attribute(Draw + "display"), "none", StringComparison.OrdinalIgnoreCase);
        var rotation = ReadRotation(element);

        if (element.Name == Draw + "frame")
        {
            if (element.Element(Draw + "image") is { } imageNode)
            {
                var rawHref = (string?)imageNode.Attribute(XLink + "href");
                var href = NormalizePackagePath(rawHref);
                var inlineData = imageNode.Element(Office + "binary-data")?.Value;
                byte[]? bytes = null;
                var assetContext = href;
                if (!string.IsNullOrWhiteSpace(inlineData))
                {
                    assetContext = $"{name} (inline binary-data)";
                    try
                    {
                        var compact = string.Concat(inlineData.Where(character => !char.IsWhiteSpace(character)));
                        bytes = Convert.FromBase64String(compact);
                        if (bytes.Length > 256L * 1024 * 1024)
                            throw new InvalidDataException("The embedded image exceeds the 256 MB asset limit.");
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidDataException)
                    {
                        issues.Add(new(InterchangeIssueSeverity.Loss, "ODF_IMAGE_BINARY_INVALID", $"Inline image '{name}' could not be decoded and was skipped: {ex.Message}", assetContext));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(href))
                {
                    bytes = readAsset(href);
                }

                if (bytes is null || bytes.Length == 0)
                {
                    issues.Add(new(InterchangeIssueSeverity.Loss, "ODF_IMAGE_MISSING", $"Image '{name}' could not be loaded and was skipped.", string.IsNullOrWhiteSpace(assetContext) ? rawHref : assetContext));
                }
                else
                {
                    var declaredMime = (string?)imageNode.Attribute(Draw + "mime-type") ?? (string?)imageNode.Attribute(Office + "mime-type");
                    var mime = ResolveImageMime(declaredMime, href, bytes);
                    if (string.IsNullOrWhiteSpace(mime))
                    {
                        issues.Add(new(InterchangeIssueSeverity.Loss, "ODF_IMAGE_FORMAT_UNSUPPORTED", $"Image '{name}' uses an unsupported or unrecognised format and was skipped.", assetContext));
                    }
                    else
                    {
                        var dataUrl = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                        var image = new ImageFrameElement
                        {
                            Name = DecorateGroupName(name, groupPath),
                            X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height,
                            Rotation = rotation, ZIndex = ++zIndex, Visible = visible,
                            DataUrl = dataUrl, OriginalDataUrl = dataUrl, AltText = name,
                            FitInsideFrame = true, BorderColor = style.Stroke, BorderWidthMm = style.StrokeWidth
                        };
                        if (mime == "image/svg+xml")
                        {
                            try
                            {
                                var svg = SvgInterchangeSanitizer.Sanitize(new UTF8Encoding(false, true).GetString(bytes));
                                var viewport = SvgInterchangeSanitizer.ReadViewport(svg);
                                image.PictureSource = new PictureDocument
                                {
                                    Name = name,
                                    WidthPx = Math.Clamp((int)Math.Round(viewport.Width), 16, 8192),
                                    HeightPx = Math.Clamp((int)Math.Round(viewport.Height), 16, 8192),
                                    Background = "transparent",
                                    Layers =
                                    [
                                        new SvgPictureLayer
                                        {
                                            Name = name, GroupPath = groupPath, SvgMarkup = svg,
                                            SourceFormat = "OpenDocument SVG", SourceElementId = assetContext,
                                            X = 0, Y = 0, Width = viewport.Width, Height = viewport.Height
                                        }
                                    ]
                                };
                            }
                            catch (Exception ex) when (ex is InvalidDataException or XmlException or DecoderFallbackException)
                            {
                                issues.Add(new(InterchangeIssueSeverity.Warning, "ODF_SVG_VECTOR_FALLBACK", $"Embedded SVG '{name}' remains placeable but could not be exposed as Picture Studio vector layers: {ex.Message}", assetContext));
                            }
                        }
                        page.Elements.Add(image);
                    }
                }
            }

            if (element.Element(Draw + "text-box") is { } textBox)
                AddTextFrame(textBox, page, style, bounds, name, groupPath, visible, rotation, ref zIndex);
            return;
        }

        if (element.Name == Draw + "rect" || element.Name == Draw + "ellipse" || element.Name == Draw + "line")
        {
            var shape = new ShapeElement
            {
                Name = DecorateGroupName(name, groupPath),
                X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height,
                Rotation = rotation, ZIndex = ++zIndex, Visible = visible,
                Shape = element.Name == Draw + "ellipse" ? PublicationShape.Ellipse : element.Name == Draw + "line" ? PublicationShape.Line : PublicationShape.Rectangle,
                Fill = element.Name == Draw + "line" ? "transparent" : style.Fill,
                Stroke = style.Stroke,
                StrokeWidth = style.StrokeWidth,
                CornerRadiusMm = ReadLengthMm((string?)element.Attribute(Draw + "corner-radius"), 0)
            };
            page.Elements.Add(shape);
            if (element.Descendants(Draw + "text-box").FirstOrDefault() is { } shapeText)
                AddTextFrame(shapeText, page, style with { Fill = "transparent", Stroke = "transparent" }, bounds, $"{name} text", groupPath, visible, rotation, ref zIndex);
            return;
        }

        if (element.Name == Draw + "path" || element.Name == Draw + "polygon" || element.Name == Draw + "polyline")
        {
            var svgMarkup = BuildShapeSvg(element, bounds, style);
            if (!string.IsNullOrWhiteSpace(svgMarkup))
            {
                var dataUrl = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svgMarkup))}";
                page.Elements.Add(new ImageFrameElement
                {
                    Name = DecorateGroupName(name, groupPath), X = bounds.X, Y = bounds.Y,
                    Width = bounds.Width, Height = bounds.Height, Rotation = rotation, ZIndex = ++zIndex, Visible = visible,
                    DataUrl = dataUrl, OriginalDataUrl = dataUrl, AltText = name, FitInsideFrame = false,
                    PictureSource = new PictureDocument
                    {
                        Name = name,
                        WidthPx = Math.Clamp((int)Math.Round(Math.Max(16, bounds.Width * 96 / 25.4)), 16, 8192),
                        HeightPx = Math.Clamp((int)Math.Round(Math.Max(16, bounds.Height * 96 / 25.4)), 16, 8192),
                        Background = "transparent",
                        Layers =
                        [
                            new SvgPictureLayer
                            {
                                Name = name, GroupPath = groupPath, SvgMarkup = svgMarkup,
                                SourceFormat = "OpenDocument vector", SourceElementId = (string?)element.Attribute(Draw + "name") ?? string.Empty,
                                X = 0, Y = 0, Width = Math.Max(1, bounds.Width * 96 / 25.4), Height = Math.Max(1, bounds.Height * 96 / 25.4)
                            }
                        ]
                    }
                });
            }
            else
            {
                issues.Add(new(InterchangeIssueSeverity.Loss, "ODF_VECTOR_UNSUPPORTED", $"Vector object '{name}' could not be represented and was skipped.", groupPath));
            }
            return;
        }

        if (element.Name == Draw + "custom-shape")
        {
            page.Elements.Add(new ShapeElement
            {
                Name = DecorateGroupName(name, groupPath), X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height,
                Rotation = rotation, ZIndex = ++zIndex, Visible = visible,
                Shape = PublicationShape.RoundedRectangle, Fill = style.Fill, Stroke = style.Stroke, StrokeWidth = style.StrokeWidth, CornerRadiusMm = 3
            });
            issues.Add(new(InterchangeIssueSeverity.Warning, "ODF_CUSTOM_SHAPE_APPROXIMATED", $"Custom shape '{name}' was approximated as a rounded rectangle.", groupPath));
            return;
        }

        if (local is "notes" or "event-listeners" or "glue-point" or "layer-set") return;
        if (element.Descendants(Draw + "text-box").FirstOrDefault() is { } nestedText)
        {
            AddTextFrame(nestedText, page, style, bounds, name, groupPath, visible, rotation, ref zIndex);
            return;
        }

        issues.Add(new(InterchangeIssueSeverity.Warning, "ODF_ELEMENT_SKIPPED", $"Unsupported OpenDocument element '{local}' was skipped.", groupPath));
    }

    private static void AddTextFrame(
        XElement textBox,
        PublicationPage page,
        ResolvedStyle style,
        Bounds bounds,
        string name,
        string groupPath,
        bool visible,
        double rotation,
        ref int zIndex)
    {
        var lines = ExtractTextLines(textBox);
        var plainText = lines.Count == 0 ? string.Empty : string.Join('\n', lines);
        var html = string.Join(string.Empty, lines.Select(line => $"<p>{WebUtility.HtmlEncode(line)}</p>"));
        if (string.IsNullOrWhiteSpace(html)) html = "<p></p>";
        page.Elements.Add(new TextFrameElement
        {
            Name = DecorateGroupName(name, groupPath),
            X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height,
            Rotation = rotation, ZIndex = ++zIndex, Visible = visible,
            PreviewHtml = html,
            DocumentContent = RichTextDocumentFactory.CreateOpenXmlFromPlainText(plainText),
            StoryFormat = StoryStorageFormat.OpenXml,
            Background = style.Fill,
            BorderColor = style.Stroke,
            BorderWidth = style.StrokeWidth,
            PaddingMm = 1.5,
            ContentFit = PublicationContentFitMode.Clip
        });
    }

    private static List<string> ExtractTextLines(XElement textBox)
    {
        var lines = new List<string>();
        foreach (var paragraph in textBox.Descendants().Where(node => node.Name == Text + "p" || node.Name == Text + "h"))
        {
            var builder = new StringBuilder();
            AppendText(paragraph, builder);
            lines.Add(builder.ToString());
        }
        return lines;
    }

    private static void AppendText(XNode node, StringBuilder builder)
    {
        if (node is XText textNode) { builder.Append(textNode.Value); return; }
        if (node is not XElement element) return;
        if (element.Name == Text + "line-break") { builder.AppendLine(); return; }
        if (element.Name == Text + "tab") { builder.Append('\t'); return; }
        if (element.Name == Text + "s")
        {
            var count = int.TryParse((string?)element.Attribute(Text + "c"), out var parsed) ? Math.Clamp(parsed, 1, 100) : 1;
            builder.Append(' ', count);
            return;
        }
        foreach (var child in element.Nodes()) AppendText(child, builder);
    }

    private static string BuildShapeSvg(XElement element, Bounds bounds, ResolvedStyle style)
    {
        var widthPx = Math.Max(1, bounds.Width * 96 / 25.4);
        var heightPx = Math.Max(1, bounds.Height * 96 / 25.4);
        var viewBox = ((string?)element.Attribute(Svg + "viewBox"))?.Trim();
        if (string.IsNullOrWhiteSpace(viewBox)) viewBox = $"0 0 {widthPx.ToString(CultureInfo.InvariantCulture)} {heightPx.ToString(CultureInfo.InvariantCulture)}";
        var fill = style.Fill == "transparent" ? "none" : style.Fill;
        var stroke = style.Stroke == "transparent" ? "none" : style.Stroke;
        var strokeWidthPx = Math.Max(0, style.StrokeWidth * 96 / 25.4).ToString("0.###", CultureInfo.InvariantCulture);
        string body;
        if (element.Name == Draw + "path")
        {
            var data = (string?)element.Attribute(Svg + "d");
            if (string.IsNullOrWhiteSpace(data)) return string.Empty;
            body = $"<path d=\"{SecurityElement.Escape(data)}\"/>";
        }
        else
        {
            var points = (string?)element.Attribute(Draw + "points") ?? (string?)element.Attribute(Svg + "points");
            if (string.IsNullOrWhiteSpace(points)) return string.Empty;
            var tag = element.Name == Draw + "polygon" ? "polygon" : "polyline";
            body = $"<{tag} points=\"{SecurityElement.Escape(points)}\"/>";
        }
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{widthPx.ToString(CultureInfo.InvariantCulture)}\" height=\"{heightPx.ToString(CultureInfo.InvariantCulture)}\" viewBox=\"{SecurityElement.Escape(viewBox)}\"><g fill=\"{SecurityElement.Escape(fill)}\" stroke=\"{SecurityElement.Escape(stroke)}\" stroke-width=\"{strokeWidthPx}\">{body}</g></svg>";
    }

    private static StyleCatalog BuildStyleCatalog(XDocument content, XDocument? stylesDocument)
    {
        var all = content.Descendants().Concat(stylesDocument?.Descendants() ?? Enumerable.Empty<XElement>());
        var catalog = new StyleCatalog();
        foreach (var node in all)
        {
            if (node.Name == Style + "style" && node.Attribute(Style + "name") is { } styleName)
                catalog.Styles[styleName.Value] = node;
            else if (node.Name == Style + "page-layout" && node.Attribute(Style + "name") is { } layoutName)
                catalog.PageLayouts[layoutName.Value] = node;
            else if (node.Name == Style + "master-page" && node.Attribute(Style + "name") is { } masterName)
                catalog.MasterPages[masterName.Value] = node;
        }
        return catalog;
    }

    private static (double Width, double Height) ResolvePageSize(XElement page, StyleCatalog styles)
    {
        var masterName = (string?)page.Attribute(Draw + "master-page-name");
        XElement? layout = null;
        if (!string.IsNullOrWhiteSpace(masterName) && styles.MasterPages.TryGetValue(masterName, out var master))
        {
            var layoutName = (string?)master.Attribute(Style + "page-layout-name");
            if (!string.IsNullOrWhiteSpace(layoutName)) styles.PageLayouts.TryGetValue(layoutName, out layout);
        }
        var properties = layout?.Element(Style + "page-layout-properties");
        var width = ReadLengthMm((string?)properties?.Attribute(Fo("page-width")), 210);
        var height = ReadLengthMm((string?)properties?.Attribute(Fo("page-height")), 297);
        return (Math.Clamp(width, 10, 5000), Math.Clamp(height, 10, 5000));
    }

    private static string ResolvePageBackground(XElement page, StyleCatalog styles)
    {
        var resolved = styles.Resolve((string?)page.Attribute(Draw + "style-name") ?? (string?)page.Attribute(Presentation + "style-name"));
        return resolved.Fill == "transparent" ? "#ffffff" : resolved.Fill;
    }

    private static Bounds ReadBounds(XElement element, PublicationPage page)
    {
        var x = ReadLengthMm((string?)element.Attribute(Svg + "x"), 0);
        var y = ReadLengthMm((string?)element.Attribute(Svg + "y"), 0);
        var width = ReadLengthMm((string?)element.Attribute(Svg + "width"), 0);
        var height = ReadLengthMm((string?)element.Attribute(Svg + "height"), 0);
        if (element.Name == Draw + "line")
        {
            var x1 = ReadLengthMm((string?)element.Attribute(Svg + "x1"), x);
            var y1 = ReadLengthMm((string?)element.Attribute(Svg + "y1"), y);
            var x2 = ReadLengthMm((string?)element.Attribute(Svg + "x2"), x + width);
            var y2 = ReadLengthMm((string?)element.Attribute(Svg + "y2"), y + height);
            x = Math.Min(x1, x2); y = Math.Min(y1, y2);
            width = Math.Max(.1, Math.Abs(x2 - x1)); height = Math.Max(.1, Math.Abs(y2 - y1));
        }
        if (width <= 0) width = Math.Min(80, page.WidthMm);
        if (height <= 0) height = Math.Min(40, page.HeightMm);
        return new Bounds(
            Math.Clamp(x, -page.WidthMm * 4, page.WidthMm * 5),
            Math.Clamp(y, -page.HeightMm * 4, page.HeightMm * 5),
            Math.Clamp(width, .1, page.WidthMm * 10),
            Math.Clamp(height, .1, page.HeightMm * 10));
    }

    private static double ReadRotation(XElement element)
    {
        var transform = (string?)element.Attribute(Draw + "transform") ?? string.Empty;
        var match = RotateRegex().Match(transform);
        if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var angle)) return 0;
        if (Math.Abs(angle) <= Math.PI * 4) angle *= 180 / Math.PI;
        return Math.Clamp(angle, -3600, 3600);
    }

    private static double ReadLengthMm(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var match = LengthRegex().Match(value.Trim());
        if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return fallback;
        return match.Groups[2].Value.ToLowerInvariant() switch
        {
            "cm" => number * 10,
            "in" => number * 25.4,
            "pt" => number * 25.4 / 72,
            "pc" => number * 25.4 / 6,
            "px" => number * 25.4 / 96,
            "mm" or "" => number,
            _ => fallback
        };
    }

    private static byte[]? ReadArchiveEntry(ZipArchive archive, string path)
    {
        var normalized = NormalizePackagePath(path);
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        var entry = archive.GetEntry(normalized);
        if (entry is null || entry.Length <= 0 || entry.Length > 256L * 1024 * 1024) return null;
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        CopyWithLimit(stream, buffer, 256L * 1024 * 1024, "The OpenDocument image asset exceeds the 256 MB import limit.");
        return buffer.ToArray();
    }


    private static string NormalizePackagePath(string? path)
    {
        var normalized = (path ?? string.Empty).Replace('\\', '/').Trim();
        while (normalized.StartsWith("./", StringComparison.Ordinal)) normalized = normalized[2..];
        normalized = normalized.TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized) || Uri.TryCreate(normalized, UriKind.Absolute, out _)) return string.Empty;
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(part => part is "." or "..")) return string.Empty;
        return string.Join('/', parts);
    }

    private static async Task CopyWithLimitAsync(Stream input, Stream output, long maximumBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0) return;
            total += read;
            if (total > maximumBytes) throw new InvalidDataException("The OpenDocument package exceeds the 512 MB import limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static string ResolveImageMime(string? declared, string path, byte[] bytes)
    {
        var normalized = (declared ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized is "image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp" or "image/svg+xml")
            return normalized;
        var byPath = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".svg" => "image/svg+xml",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".png" => "image/png",
            _ => string.Empty
        };
        if (!string.IsNullOrWhiteSpace(byPath)) return byPath;
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return "image/jpeg";
        if (bytes.Length >= 6 && (Encoding.ASCII.GetString(bytes, 0, 6) is "GIF87a" or "GIF89a")) return "image/gif";
        var head = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 512)).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || head.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && head.Contains("<svg", StringComparison.OrdinalIgnoreCase)) return "image/svg+xml";
        return string.Empty;
    }

    private static void CopyWithLimit(Stream input, Stream output, long maximumBytes, string message)
    {
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = input.Read(buffer, 0, buffer.Length);
            if (read == 0) return;
            total += read;
            if (total > maximumBytes) throw new InvalidDataException(message);
            output.Write(buffer, 0, read);
        }
    }

    private static async Task<XDocument> LoadXmlAsync(Stream source, CancellationToken cancellationToken)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = 128L * 1024 * 1024
        };
        using var reader = XmlReader.Create(source, settings);
        return await XDocument.LoadAsync(reader, LoadOptions.PreserveWhitespace, cancellationToken);
    }

    private static string DecorateGroupName(string name, string groupPath) => string.IsNullOrWhiteSpace(groupPath) ? name : $"{groupPath} / {name}";
    private static string CleanName(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static XName Fo(string localName) => XName.Get(localName, "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0");

    private sealed class StyleCatalog
    {
        public Dictionary<string, XElement> Styles { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, XElement> PageLayouts { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, XElement> MasterPages { get; } = new(StringComparer.Ordinal);

        public ResolvedStyle Resolve(string? name)
        {
            var chain = new List<XElement>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var currentName = name;
            while (!string.IsNullOrWhiteSpace(currentName) && visited.Add(currentName) && Styles.TryGetValue(currentName, out var style))
            {
                chain.Add(style);
                currentName = (string?)style.Attribute(Style + "parent-style-name");
            }

            // ODF child styles override their parent. Apply the chain from the oldest parent
            // toward the requested style instead of accidentally letting a parent win last.
            chain.Reverse();
            var fill = "transparent";
            var stroke = "transparent";
            var strokeWidth = 0d;
            foreach (var style in chain)
            {
                foreach (var properties in style.Elements().Where(node => node.Name.LocalName.EndsWith("properties", StringComparison.Ordinal)))
                {
                    var fillMode = (string?)properties.Attribute(Draw + "fill");
                    var fillColor = (string?)properties.Attribute(Draw + "fill-color");
                    if (string.Equals(fillMode, "none", StringComparison.OrdinalIgnoreCase)) fill = "transparent";
                    else if (!string.IsNullOrWhiteSpace(fillColor)) fill = NormalizeColor(fillColor, fill);
                    var strokeMode = (string?)properties.Attribute(Draw + "stroke");
                    var strokeColor = (string?)properties.Attribute(Svg + "stroke-color");
                    if (string.Equals(strokeMode, "none", StringComparison.OrdinalIgnoreCase)) stroke = "transparent";
                    else if (!string.IsNullOrWhiteSpace(strokeColor)) stroke = NormalizeColor(strokeColor, stroke);
                    var width = (string?)properties.Attribute(Svg + "stroke-width");
                    if (!string.IsNullOrWhiteSpace(width)) strokeWidth = ReadLengthMm(width, strokeWidth);
                }
            }
            return new ResolvedStyle(fill, stroke, Math.Clamp(strokeWidth, 0, 20));
        }
    }

    private static string NormalizeColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var color = value.Trim();
        return ColorRegex().IsMatch(color) ? color : fallback;
    }

    private readonly record struct Bounds(double X, double Y, double Width, double Height);
    private readonly record struct ResolvedStyle(string Fill, string Stroke, double StrokeWidth);

    [GeneratedRegex(@"^([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?)\s*(mm|cm|in|pt|pc|px)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LengthRegex();
    [GeneratedRegex(@"rotate\s*\(\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RotateRegex();
    [GeneratedRegex(@"^(?:#[0-9a-f]{3,8}|rgba?\([^)]*\)|hsla?\([^)]*\)|transparent)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();
}
