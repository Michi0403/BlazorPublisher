using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Documentation;

/// <summary>
/// Reads build-generated DocFX artifacts and compiler XML comments from the installed application tree.
/// </summary>
public sealed class PublisherDocumentationCatalogService(
    IWebHostEnvironment environment,
    ILogger<PublisherDocumentationCatalogService> logger) : IPublisherDocumentationCatalogService
{
    private readonly object synchronization = new();
    private IReadOnlyList<PublisherDocumentationComment>? commentCache;
    private string? commentCachePath;
    private DateTime commentCacheWriteUtc;

    /// <inheritdoc />
    public PublisherDocumentationStatus GetStatus()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
        var root = ResolveDocumentationRoot();
        var manifest = ReadManifest(root);
        var comments = LoadComments(root);
        var pdfPath = GetPdfPath();
        return new PublisherDocumentationStatus
        {
            Version = manifest?.Version ?? version,
            GeneratedAtUtc = manifest?.GeneratedAtUtc,
            HtmlAvailable = root is not null && File.Exists(Path.Combine(root, "index.html")),
            PdfAvailable = pdfPath is not null,
            XmlCommentsAvailable = comments.Count > 0,
            CommentCount = comments.Count,
            PdfFileName = pdfPath is null ? $"PublisherStudio-{version}.pdf" : Path.GetFileName(pdfPath)
        };
    }

    /// <inheritdoc />
    public string? GetHtmlFilePath(string? relativePath)
    {
        var root = ResolveDocumentationRoot();
        if (root is null) return null;

        var normalized = string.IsNullOrWhiteSpace(relativePath)
            ? "index.html"
            : relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Length == 0) normalized = "index.html";

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or "..")) return null;

        var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(root, Path.Combine(segments)));
        if (!candidate.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)) return null;
        if (Directory.Exists(candidate)) candidate = Path.Combine(candidate, "index.html");
        return File.Exists(candidate) ? candidate : null;
    }

    /// <inheritdoc />
    public string? GetPdfPath()
    {
        var root = ResolveDocumentationRoot();
        if (root is null) return null;
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty;
        var exact = Path.Combine(root, $"PublisherStudio-{version}.pdf");
        if (File.Exists(exact)) return exact;
        return Directory.EnumerateFiles(root, "PublisherStudio-*.pdf", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public IReadOnlyList<PublisherDocumentationComment> SearchComments(string? query, int limit)
    {
        var boundedLimit = Math.Clamp(limit, 1, 500);
        var comments = LoadComments(ResolveDocumentationRoot());
        if (string.IsNullOrWhiteSpace(query)) return comments.Take(boundedLimit).ToArray();
        var search = query.Trim();
        return comments
            .Where(comment => CommentMatches(comment, search))
            .Take(boundedLimit)
            .ToArray();
    }

    private IReadOnlyList<PublisherDocumentationComment> LoadComments(string? documentationRoot)
    {
        var path = ResolveXmlDocumentationPath(documentationRoot);
        if (path is null) return [];
        var writeUtc = File.GetLastWriteTimeUtc(path);
        lock (synchronization)
        {
            if (commentCache is not null &&
                string.Equals(commentCachePath, path, StringComparison.OrdinalIgnoreCase) &&
                commentCacheWriteUtc == writeUtc)
                return commentCache;

            try
            {
                var document = XDocument.Load(path, LoadOptions.None);
                commentCache = document.Root?.Element("members")?.Elements("member")
                    .Select(CreateComment)
                    .Where(comment => comment is not null)
                    .Cast<PublisherDocumentationComment>()
                    .OrderBy(comment => comment.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? [];
                commentCachePath = path;
                commentCacheWriteUtc = writeUtc;
                logger.LogInformation("Loaded {CommentCount} PublisherStudio XML documentation members.", commentCache.Count);
                return commentCache;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
            {
                logger.LogWarning(exception, "PublisherStudio XML documentation could not be loaded.");
                return [];
            }
        }
    }

    private PublisherDocumentationComment? CreateComment(XElement member)
    {
        var memberId = member.Attribute("name")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(memberId)) return null;
        var summary = NormalizeComment(member.Element("summary")?.Value);
        var remarks = NormalizeComment(member.Element("remarks")?.Value);
        if (summary.Length == 0 && remarks.Length == 0) return null;
        return new PublisherDocumentationComment
        {
            MemberId = memberId,
            DisplayName = BuildDisplayName(memberId),
            Summary = summary,
            Remarks = remarks
        };
    }

    private bool CommentMatches(PublisherDocumentationComment comment, string query) =>
        comment.MemberId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        comment.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        comment.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        comment.Remarks.Contains(query, StringComparison.OrdinalIgnoreCase);

    private string? ResolveDocumentationRoot()
    {
        var candidates = new[]
        {
            Path.Combine(environment.WebRootPath ?? string.Empty, "help-docs"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "help-docs"),
            Path.Combine(environment.ContentRootPath, "wwwroot", "help-docs")
        };
        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(path => File.Exists(Path.Combine(path, "index.html")));
    }

    private string? ResolveXmlDocumentationPath(string? documentationRoot)
    {
        var candidates = new[]
        {
            documentationRoot is null ? null : Path.Combine(documentationRoot, "PublisherStudio.Web.xml"),
            Path.Combine(AppContext.BaseDirectory, "PublisherStudio.Web.xml"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "help-docs", "PublisherStudio.Web.xml")
        };
        return candidates.FirstOrDefault(path => path is not null && File.Exists(path));
    }

    private PublisherDocumentationManifest? ReadManifest(string? documentationRoot)
    {
        if (documentationRoot is null) return null;
        var path = Path.Combine(documentationRoot, "documentation-status.json");
        if (!File.Exists(path)) return null;
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<PublisherDocumentationManifest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(exception, "PublisherStudio documentation status could not be read.");
            return null;
        }
    }

    private string BuildDisplayName(string memberId)
    {
        var value = memberId.Length > 2 && memberId[1] == ':' ? memberId[2..] : memberId;
        var parameter = value.IndexOf('(');
        if (parameter >= 0) value = value[..parameter];
        return value.Replace('#', '.');
    }

    private string NormalizeComment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}
