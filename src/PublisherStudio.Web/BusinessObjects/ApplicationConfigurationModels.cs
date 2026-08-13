namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Carries the configurable PublisherStudio path settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PublisherStudioPathOptions
{
    /// <summary>
    /// Gets or sets the images value that forms part of the PublisherStudio path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The images value exposed by <see cref="PublisherStudioPathOptions"/>.</value>
    public string Images { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the video value that forms part of the PublisherStudio path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The video value exposed by <see cref="PublisherStudioPathOptions"/>.</value>
    public string Video { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the audio value that forms part of the PublisherStudio path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The audio value exposed by <see cref="PublisherStudioPathOptions"/>.</value>
    public string Audio { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the documents value that forms part of the PublisherStudio path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The documents value exposed by <see cref="PublisherStudioPathOptions"/>.</value>
    public string Documents { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the exports value that forms part of the PublisherStudio path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The exports value exposed by <see cref="PublisherStudioPathOptions"/>.</value>
    public string Exports { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the open OpenSCAD value that forms part of the PublisherStudio path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The open OpenSCAD value exposed by <see cref="PublisherStudioPathOptions"/>.</value>
    public string OpenScad { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the projects value that forms part of the PublisherStudio path state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The projects value exposed by <see cref="PublisherStudioPathOptions"/>.</value>
    public string Projects { get; set; } = string.Empty;
}

/// <summary>
/// Carries the configurable publication project settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PublicationProjectSettings
{
    /// <summary>
    /// Gets or sets the culture value that forms part of the publication project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The culture value exposed by <see cref="PublicationProjectSettings"/>.</value>
    public string Culture { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the paths used by this publication project instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The paths value exposed by <see cref="PublicationProjectSettings"/>.</value>
    public PublisherStudioPathOptions Paths { get; set; } = new();
    /// <summary>
    /// Gets or sets the default render format value that forms part of the publication project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default render format value exposed by <see cref="PublicationProjectSettings"/>.</value>
    public string DefaultRenderFormat { get; set; } = "png";
    /// <summary>
    /// Gets or sets the default render DPI value that forms part of the publication project state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default render DPI value exposed by <see cref="PublicationProjectSettings"/>.</value>
    public int DefaultRenderDpi { get; set; } = 150;
    /// <summary>
    /// Gets or sets a value indicating whether prefer rendered still exports applies to the publication project state.
    /// </summary>
    /// <value>The prefer rendered still exports value exposed by <see cref="PublicationProjectSettings"/>.</value>
    public bool PreferRenderedStillExports { get; set; } = true;
}

/// <summary>
/// Represents a render export capability application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RenderExportCapability
{
    /// <summary>
    /// Gets or sets the format value that forms part of the render export capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format value exposed by <see cref="RenderExportCapability"/>.</value>
    public string Format { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the MIME type value that forms part of the render export capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MIME type value exposed by <see cref="RenderExportCapability"/>.</value>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether captures video frames applies to the render export capability state.
    /// </summary>
    /// <value>The captures video frames value exposed by <see cref="RenderExportCapability"/>.</value>
    public bool CapturesVideoFrames { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether captures canvas effects applies to the render export capability state.
    /// </summary>
    /// <value>The captures canvas effects value exposed by <see cref="RenderExportCapability"/>.</value>
    public bool CapturesCanvasEffects { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether preserves vector content applies to the render export capability state.
    /// </summary>
    /// <value>The preserves vector content value exposed by <see cref="RenderExportCapability"/>.</value>
    public bool PreservesVectorContent { get; set; }
    /// <summary>
    /// Gets or sets the HTML support value that forms part of the render export capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML support value exposed by <see cref="RenderExportCapability"/>.</value>
    public PublicationHtmlExportSupport HtmlSupport { get; set; }
    /// <summary>
    /// Gets or sets the note value that forms part of the render export capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The note value exposed by <see cref="RenderExportCapability"/>.</value>
    public string Note { get; set; } = string.Empty;
}
