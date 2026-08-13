using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates render export catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RenderExportCatalogService(ILogger<RenderExportCatalogService> logger) : IRenderExportCatalogService
{
    /// <summary>
    /// Stores the in-memory capabilities collection maintained internally by <see cref="RenderExportCatalogService"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<RenderExportCapability> _capabilities = new List<RenderExportCapability>
    {
        new() { Format = "png", MimeType = "image/png", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = false, HtmlSupport = PublicationHtmlExportSupport.CanvasRuntime, Note = "Rendered still frame with live video/effect canvases frozen at export time." },
        new() { Format = "jpg", MimeType = "image/jpeg", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = false, HtmlSupport = PublicationHtmlExportSupport.CanvasRuntime, Note = "Rendered opaque still frame; transparency is flattened." },
        new() { Format = "svg", MimeType = "image/svg+xml", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = true, HtmlSupport = PublicationHtmlExportSupport.CanvasRuntime, Note = "SVG foreignObject export; live media and effect canvases are embedded as frozen images." },
        new() { Format = "html", MimeType = "text/html", CapturesVideoFrames = false, CapturesCanvasEffects = true, PreservesVectorContent = true, HtmlSupport = PublicationHtmlExportSupport.Native, Note = "Interactive export. Effects marked RenderBeforeExport require baking first." },
        new() { Format = "pdf", MimeType = "application/pdf", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = false, HtmlSupport = PublicationHtmlExportSupport.RenderBeforeExport, Note = "Browser print path; dynamic media is represented by its current rendered frame." }
    }.AsReadOnly();

    /// <summary>
    /// Retrieves capabilities as part of the render export catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<RenderExportCapability> GetCapabilities() {
        try
        {
            logger.LogTrace($"Entering RenderExportCatalogService.GetCapabilities.");
            return _capabilities;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"RenderExportCatalogService.GetCapabilities failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Performs find as part of the render export catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="format">Format value supplied to the render export catalog operation and used when producing its result.</param>
    /// <returns>The render export capability produced by the operation.</returns>
    public RenderExportCapability? Find(string format) {
        try
        {
            logger.LogTrace($"Entering RenderExportCatalogService.Find.");
            return _capabilities.FirstOrDefault(item => string.Equals(item.Format, format?.TrimStart('.'), StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"RenderExportCatalogService.Find failed: {exception.Message}");
            throw;
        }
    }
}
