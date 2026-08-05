namespace PublisherStudio.BusinessObjects;

// logging-policy: pure-helper
/// <summary>
/// Represents a panel text pattern store options.
/// </summary>
public sealed class PanelTextPatternStoreOptions
{
    /// <summary>
    /// Gets or sets seed path.
    /// </summary>
    public string SeedPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets override directory name.
    /// </summary>
    public string OverrideDirectoryName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets override file name.
    /// </summary>
    public string OverrideFileName { get; set; } = string.Empty;
}
