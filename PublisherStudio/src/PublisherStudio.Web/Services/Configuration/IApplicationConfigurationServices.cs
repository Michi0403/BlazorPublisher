using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the application path service contract.
/// </summary>
public interface IApplicationPathService
{
    PublisherStudioPathOptions GetDefaults();
    PublisherStudioPathOptions Resolve(PublisherStudioPathOptions? projectOverrides = null);
    string ResolveMediaPath(string mediaKind, PublisherStudioPathOptions? projectOverrides = null);
    void EnsureDirectories(PublisherStudioPathOptions? projectOverrides = null);
}

/// <summary>
/// Defines the file localization service contract.
/// </summary>
public interface IFileLocalizationService
{
    IReadOnlyList<string> GetAvailableCultures();
    IReadOnlyDictionary<string, string> GetStrings(string? culture = null);
    string Get(string key, string? culture = null, string? fallback = null);
    Task SaveOverridesAsync(string culture, IReadOnlyDictionary<string, string> strings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the render export catalog service contract.
/// </summary>
public interface IRenderExportCatalogService
{
    IReadOnlyList<RenderExportCapability> GetCapabilities();
    RenderExportCapability? Find(string format);
}
