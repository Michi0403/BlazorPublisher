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

/// <summary>Describes the effective PublisherStudio folder contract detected for the current host and user.</summary>
public sealed class PublisherStudioApplicationPathLayout
{
    /// <summary>Gets the detected host platform associated with the effective path-layout snapshot used by support and first-boot guidance.</summary>
    /// <value>The detected platform name.</value>
    public string Platform { get; init; } = string.Empty;
    /// <summary>Gets the canonical per-user writable PublisherStudio data root used by mutable application state.</summary>
    /// <value>The absolute per-user application-data root.</value>
    public string UserDataRoot { get; init; } = string.Empty;
    /// <summary>Gets the per-user configuration overlay file used by PublisherStudio startup configuration.</summary>
    /// <value>The absolute path to appsettings.user.json.</value>
    public string UserConfigurationFile { get; init; } = string.Empty;
    /// <summary>Gets the persisted per-user system-variable file used by PublisherStudio configuration services.</summary>
    /// <value>The absolute path to the system-variable store.</value>
    public string SystemVariablesFile { get; init; } = string.Empty;
    /// <summary>Gets the per-user runtime directory used by endpoint and transient runtime state.</summary>
    /// <value>The absolute runtime directory path.</value>
    public string RuntimeDirectory { get; init; } = string.Empty;
    /// <summary>Gets the per-user data-protection directory used by ASP.NET cryptographic key persistence.</summary>
    /// <value>The absolute data-protection directory path.</value>
    public string DataProtectionDirectory { get; init; } = string.Empty;
    /// <summary>Gets the per-user spreadsheet hibernation directory used by recoverable spreadsheet state.</summary>
    /// <value>The absolute spreadsheet hibernation directory path.</value>
    public string SpreadsheetHibernationDirectory { get; init; } = string.Empty;
    /// <summary>Gets the resolved default content-path collection used by PublisherStudio when no project override is supplied.</summary>
    /// <value>The default content path options for the current user.</value>
    public PublisherStudioPathOptions DefaultContentPaths { get; init; } = new();
    /// <summary>Gets the application base directory used by explicit portable content and tool discovery.</summary>
    /// <value>The absolute application base directory.</value>
    public string PortableApplicationRoot { get; init; } = string.Empty;
    /// <summary>Gets the system-wide discovery-root collection used to locate shared installations without making them writable defaults.</summary>
    /// <value>The ordered system-wide discovery roots for the current host.</value>
    public IReadOnlyList<string> SystemWideDiscoveryRoots { get; init; } = Array.Empty<string>();
    /// <summary>Gets the persisted path-layout report file used by first-boot and support diagnostics.</summary>
    /// <value>The absolute path to path-layout.json.</value>
    public string LayoutReportFile { get; init; } = string.Empty;
    /// <summary>Gets a value indicating whether this layout snapshot was produced before an existing path-layout report was found.</summary>
    /// <value><see langword="true"/> when first boot was detected for the report; otherwise <see langword="false"/>.</value>
    public bool FirstBootDetected { get; init; }
    /// <summary>Gets the UTC timestamp associated with creation of the current path-layout snapshot.</summary>
    /// <value>The layout generation timestamp in UTC.</value>
    public DateTime GeneratedAtUtc { get; init; }
}
