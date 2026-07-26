using PublisherStudio.Domain;

namespace PublisherStudio.Services.Configuration;

public interface IApplicationPathService
{
    PublisherStudioPathOptions GetDefaults();
    PublisherStudioPathOptions Resolve(PublisherStudioPathOptions? projectOverrides = null);
    string ResolveMediaPath(string mediaKind, PublisherStudioPathOptions? projectOverrides = null);
    void EnsureDirectories(PublisherStudioPathOptions? projectOverrides = null);
}

public interface IFileLocalizationService
{
    IReadOnlyList<string> GetAvailableCultures();
    IReadOnlyDictionary<string, string> GetStrings(string? culture = null);
    string Get(string key, string? culture = null, string? fallback = null);
    Task SaveOverridesAsync(string culture, IReadOnlyDictionary<string, string> strings, CancellationToken cancellationToken = default);
}

public interface IRenderExportCatalogService
{
    IReadOnlyList<RenderExportCapability> GetCapabilities();
    RenderExportCapability? Find(string format);
}
