namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents a publisher studio path options.
/// </summary>
public sealed class PublisherStudioPathOptions
{
    /// <summary>
    /// Gets or sets images.
    /// </summary>
    public string Images { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets video.
    /// </summary>
    public string Video { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets audio.
    /// </summary>
    public string Audio { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets documents.
    /// </summary>
    public string Documents { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets exports.
    /// </summary>
    public string Exports { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets open scad.
    /// </summary>
    public string OpenScad { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets projects.
    /// </summary>
    public string Projects { get; set; } = string.Empty;
}

/// <summary>
/// Represents a publication project settings.
/// </summary>
public sealed class PublicationProjectSettings
{
    /// <summary>
    /// Gets or sets culture.
    /// </summary>
    public string Culture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets paths.
    /// </summary>
    public PublisherStudioPathOptions Paths { get; set; } = new();
    /// <summary>
    /// Gets or sets default render format.
    /// </summary>
    public string DefaultRenderFormat { get; set; } = "png";
    /// <summary>
    /// Gets or sets default render DPI.
    /// </summary>
    public int DefaultRenderDpi { get; set; } = 150;
    /// <summary>
    /// Gets or sets prefer rendered still exports.
    /// </summary>
    public bool PreferRenderedStillExports { get; set; } = true;
}

/// <summary>
/// Represents a render export capability.
/// </summary>
public sealed class RenderExportCapability
{
    /// <summary>
    /// Gets or sets format.
    /// </summary>
    public string Format { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mime type.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets captures video frames.
    /// </summary>
    public bool CapturesVideoFrames { get; set; }
    /// <summary>
    /// Gets or sets captures canvas effects.
    /// </summary>
    public bool CapturesCanvasEffects { get; set; }
    /// <summary>
    /// Gets or sets preserves vector content.
    /// </summary>
    public bool PreservesVectorContent { get; set; }
    /// <summary>
    /// Gets or sets HTML support.
    /// </summary>
    public PublicationHtmlExportSupport HtmlSupport { get; set; }
    /// <summary>
    /// Gets or sets note.
    /// </summary>
    public string Note { get; set; } = string.Empty;
}
