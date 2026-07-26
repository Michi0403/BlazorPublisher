using PublisherStudio.Domain;

namespace PublisherStudio.Services.Configuration;

public sealed class RenderExportCatalogService : IRenderExportCatalogService
{
    private readonly IReadOnlyList<RenderExportCapability> _capabilities = new List<RenderExportCapability>
    {
        new() { Format = "png", MimeType = "image/png", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = false, HtmlSupport = PublicationHtmlExportSupport.CanvasRuntime, Note = "Rendered still frame with live video/effect canvases frozen at export time." },
        new() { Format = "jpg", MimeType = "image/jpeg", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = false, HtmlSupport = PublicationHtmlExportSupport.CanvasRuntime, Note = "Rendered opaque still frame; transparency is flattened." },
        new() { Format = "svg", MimeType = "image/svg+xml", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = true, HtmlSupport = PublicationHtmlExportSupport.CanvasRuntime, Note = "SVG foreignObject export; live media and effect canvases are embedded as frozen images." },
        new() { Format = "html", MimeType = "text/html", CapturesVideoFrames = false, CapturesCanvasEffects = true, PreservesVectorContent = true, HtmlSupport = PublicationHtmlExportSupport.Native, Note = "Interactive export. Effects marked RenderBeforeExport require baking first." },
        new() { Format = "pdf", MimeType = "application/pdf", CapturesVideoFrames = true, CapturesCanvasEffects = true, PreservesVectorContent = false, HtmlSupport = PublicationHtmlExportSupport.RenderBeforeExport, Note = "Browser print path; dynamic media is represented by its current rendered frame." }
    }.AsReadOnly();

    public IReadOnlyList<RenderExportCapability> GetCapabilities() => _capabilities;
    public RenderExportCapability? Find(string format) => _capabilities.FirstOrDefault(item => string.Equals(item.Format, format?.TrimStart('.'), StringComparison.OrdinalIgnoreCase));
}
