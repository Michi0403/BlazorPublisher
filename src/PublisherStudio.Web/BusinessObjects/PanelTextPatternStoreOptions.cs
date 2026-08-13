namespace PublisherStudio.BusinessObjects;

// logging-policy: pure-helper
/// <summary>
/// Carries the configurable panel text pattern store settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class PanelTextPatternStoreOptions
{
    /// <summary>
    /// Gets or sets the seed path used by this panel text pattern store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The seed path value exposed by <see cref="PanelTextPatternStoreOptions"/>.</value>
    public string SeedPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the override directory name used by this panel text pattern store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The override directory name value exposed by <see cref="PanelTextPatternStoreOptions"/>.</value>
    public string OverrideDirectoryName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the override file name used by this panel text pattern store instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The override file name value exposed by <see cref="PanelTextPatternStoreOptions"/>.</value>
    public string OverrideFileName { get; set; } = string.Empty;
}
