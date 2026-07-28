namespace PublisherStudio.Services.Configuration;

// logging-policy: pure-helper
public sealed class PanelTextPatternStoreOptions
{
    public const string SectionName = "PublisherStudio:RuntimeValueStores:PanelTextPatterns";
    public string SeedPath { get; set; } = string.Empty;
    public string OverrideDirectoryName { get; set; } = string.Empty;
    public string OverrideFileName { get; set; } = string.Empty;
}
