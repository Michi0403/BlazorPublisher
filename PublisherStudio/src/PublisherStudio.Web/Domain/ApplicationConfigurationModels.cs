namespace PublisherStudio.Domain;

public sealed class PublisherStudioPathOptions
{
    public string Images { get; set; } = string.Empty;
    public string Video { get; set; } = string.Empty;
    public string Audio { get; set; } = string.Empty;
    public string Documents { get; set; } = string.Empty;
    public string Exports { get; set; } = string.Empty;
    public string OpenScad { get; set; } = string.Empty;
    public string Projects { get; set; } = string.Empty;
}

public sealed class PublicationProjectSettings
{
    public string Culture { get; set; } = string.Empty;
    public PublisherStudioPathOptions Paths { get; set; } = new();
    public string DefaultRenderFormat { get; set; } = "png";
    public int DefaultRenderDpi { get; set; } = 150;
    public bool PreferRenderedStillExports { get; set; } = true;
}

public sealed class RenderExportCapability
{
    public string Format { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public bool CapturesVideoFrames { get; set; }
    public bool CapturesCanvasEffects { get; set; }
    public bool PreservesVectorContent { get; set; }
    public PublicationHtmlExportSupport HtmlSupport { get; set; }
    public string Note { get; set; } = string.Empty;
}
