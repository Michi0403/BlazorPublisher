using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Documentation;

/// <summary>
/// Reads build-generated DocFX artifacts and compiler XML comments from the installed application tree.
/// </summary>
/// <param name="environment">Web host environment dependency used by the publisher documentation catalog workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="platform">Publisher platform runtime service dependency used by the publisher documentation catalog workflow to provide the corresponding application capability.</param>
public sealed class PublisherDocumentationCatalogService(
    IWebHostEnvironment environment,
    IPublisherPlatformRuntimeService platform,
    ILogger<PublisherDocumentationCatalogService> logger) : IPublisherDocumentationCatalogService
{
    /// <summary>
    /// Stores the internal synchronization state used by <see cref="PublisherDocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object synchronization = new();
    /// <summary>
    /// Stores the in-memory comment cache collection maintained internally by <see cref="PublisherDocumentationCatalogService"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<PublisherDocumentationComment>? commentCache;
    /// <summary>
    /// Stores the internal comment cache path state used by <see cref="PublisherDocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private string? commentCachePath;
    /// <summary>
    /// Stores the internal comment cache write UTC state used by <see cref="PublisherDocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private DateTime commentCacheWriteUtc;

    /// <summary>
    /// Retrieves status as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public PublisherDocumentationStatus GetStatus()
    {
    try
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
                HtmlUrl = "/api/documentation/html/index.html",
                PdfUrl = "/api/documentation/pdf",
                CommentsUrl = "/api/documentation/comments",
                PdfFileName = pdfPath is null ? $"PublisherStudio-{version}.pdf" : Path.GetFileName(pdfPath)
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(GetStatus)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(GetStatus)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves HTML file path as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string? GetHtmlFilePath(string? relativePath)
    {
    try
    {
            var root = ResolveDocumentationRoot();
            if (root is null) return null;

            var normalized = string.IsNullOrWhiteSpace(relativePath)
                ? "index.html"
                : relativePath.Replace('\\', '/').TrimStart('/');
            if (normalized.Length == 0) normalized = "index.html";

            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment is "." or "..")) return null;

            var rootPath = Path.GetFullPath(root);
            var candidate = Path.GetFullPath(Path.Combine(root, Path.Combine(segments)));
            if (!platform.IsSameOrDescendantPath(rootPath, candidate)) return null;
            if (Directory.Exists(candidate)) candidate = Path.Combine(candidate, "index.html");
            return File.Exists(candidate) ? candidate : null;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(GetHtmlFilePath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(GetHtmlFilePath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves PDF path as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string? GetPdfPath()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(GetPdfPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(GetPdfPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Searches comments as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<PublisherDocumentationComment> SearchComments(string? query, int limit)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(SearchComments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(SearchComments)} failed.");
        throw;
    }
}

    /// <summary>
    /// Loads comments as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentationRoot">Documentation root value supplied to the publisher documentation catalog operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<PublisherDocumentationComment> LoadComments(string? documentationRoot)
    {
        var path = ResolveXmlDocumentationPath(documentationRoot);
        if (path is null) return [];
        var writeUtc = File.GetLastWriteTimeUtc(path);
        lock (synchronization)
        {
            if (commentCache is not null &&
                platform.PathsEqual(commentCachePath, path) &&
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

    /// <summary>
    /// Creates comment as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="member">Member value supplied to the publisher documentation catalog operation and used when producing its result.</param>
    /// <returns>The publisher documentation comment produced by the operation.</returns>
    private PublisherDocumentationComment? CreateComment(XElement member)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(CreateComment)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(CreateComment)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs comment matches as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="comment">Comment value supplied to the publisher documentation catalog operation and used when producing its result.</param>
    /// <param name="query">Query value supplied to the publisher documentation catalog operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool CommentMatches(PublisherDocumentationComment comment, string query) {
    try
    {
        return comment.MemberId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        comment.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        comment.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        comment.Remarks.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(CommentMatches)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(CommentMatches)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves documentation root as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    private string? ResolveDocumentationRoot()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(ResolveDocumentationRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(ResolveDocumentationRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves XML documentation path as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentationRoot">Documentation root value supplied to the publisher documentation catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? ResolveXmlDocumentationPath(string? documentationRoot)
    {
    try
    {
            var candidates = new[]
            {
                documentationRoot is null ? null : Path.Combine(documentationRoot, "PublisherStudio.Web.xml"),
                Path.Combine(AppContext.BaseDirectory, "PublisherStudio.Web.xml"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "help-docs", "PublisherStudio.Web.xml")
            };
            return candidates.FirstOrDefault(path => path is not null && File.Exists(path));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(ResolveXmlDocumentationPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(ResolveXmlDocumentationPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads manifest as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentationRoot">Documentation root value supplied to the publisher documentation catalog operation and used when producing its result.</param>
    /// <returns>The publisher documentation manifest produced by the operation.</returns>
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

    /// <summary>
    /// Builds display name as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="memberId">Identifier of the member to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildDisplayName(string memberId)
    {
    try
    {
            var value = memberId.Length > 2 && memberId[1] == ':' ? memberId[2..] : memberId;
            var parameter = value.IndexOf('(');
            if (parameter >= 0) value = value[..parameter];
            return value.Replace('#', '.');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(BuildDisplayName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(BuildDisplayName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes comment as part of the publisher documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publisher documentation catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeComment(string? value)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(NormalizeComment)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationCatalogService)}.{nameof(NormalizeComment)} failed.");
        throw;
    }
}
}
