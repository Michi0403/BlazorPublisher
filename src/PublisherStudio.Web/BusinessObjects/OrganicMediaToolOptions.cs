namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Configures the 1-Wire media and export tools (appsettings section <c>OrganicPlugins:MediaTools</c>).
/// Limits, timeouts and the peer argument policy are application settings so no handler carries a fixed value.
/// </summary>
public sealed class OrganicMediaToolOptions
{
    /// <summary>Gets or sets how many recent conversion jobs <c>publisher.media.jobs</c> returns.</summary>
    /// <value>Defaults to 50.</value>
    public int MaximumListedJobs { get; set; } = 50;

    /// <summary>Gets or sets how long the owning editor may take to produce an approved publication export, in seconds.</summary>
    /// <value>Defaults to 120 because multi-page raster and website exports render every page.</value>
    public int EditorExportTimeoutSeconds { get; set; } = 120;

    /// <summary>Gets or sets the raster resolution used when a 1-Wire export request names no dpi.</summary>
    /// <value>Defaults to 150.</value>
    public int DefaultExportDpi { get; set; } = 150;

    /// <summary>
    /// Gets or sets a value indicating whether a linked peer may send FFmpeg filter graphs or advanced arguments.
    /// Filters can read local files, so peers normally use presets or operator-saved profiles instead.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool AllowPeerFilterAndAdvancedArguments { get; set; }

    /// <summary>Gets or sets the largest exported or converted file that can be read in chunks over 1-Wire, in bytes.</summary>
    /// <value>Defaults to 256 MiB; files that fit one 1-Wire message are returned inline.</value>
    public int MaximumTransferFileBytes { get; set; } = 256 * 1024 * 1024;

    /// <summary>Gets or sets how long an approved trim can wait for its file before reporting a timeout.</summary>
    /// <value>The wait in seconds. The job remains available in Media Converter Studio.</value>
    public int RemoteTrimTimeoutSeconds { get; set; } = 120;

    /// <summary>Gets or sets how many bytes of announced files may wait to be read at the same time.</summary>
    /// <value>Defaults to 512 MiB.</value>
    public long MaximumRetainedTransferBytes { get; set; } = 512L * 1024 * 1024;

    /// <summary>Gets or sets how long an announced file stays readable, in minutes.</summary>
    /// <value>Defaults to 30.</value>
    public int TransferRetentionMinutes { get; set; } = 30;
}
