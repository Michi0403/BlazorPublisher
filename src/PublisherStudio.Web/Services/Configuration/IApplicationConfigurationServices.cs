using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the application path service contract.
/// </summary>
public interface IApplicationPathService
{
    /// <summary>
    /// Gets defaults.
    /// </summary>
    PublisherStudioPathOptions GetDefaults();
    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
    PublisherStudioPathOptions Resolve(PublisherStudioPathOptions? projectOverrides = null);
    /// <summary>
    /// Resolves media path.
    /// </summary>
    string ResolveMediaPath(string mediaKind, PublisherStudioPathOptions? projectOverrides = null);
    /// <summary>
    /// Ensures directories.
    /// </summary>
    void EnsureDirectories(PublisherStudioPathOptions? projectOverrides = null);
}

/// <summary>
/// Defines the file localization service contract.
/// </summary>
public interface IFileLocalizationService
{
    /// <summary>Gets all complete built-in and user-overridden localization cultures.</summary>
    IReadOnlyList<string> GetAvailableCultures();

    /// <summary>Gets the effective flat JSON string dictionary for one culture.</summary>
    IReadOnlyDictionary<string, string> GetStrings(string? culture = null);

    /// <summary>Gets one localized string with English and caller-provided fallback behavior.</summary>
    string Get(string key, string? culture = null, string? fallback = null);

    /// <summary>Resolves a requested culture to one complete PublisherStudio catalog.</summary>
    string ResolveAvailableCulture(string? culture);

    /// <summary>Gets the native display label for one available culture.</summary>
    string GetCultureDisplayName(string culture);

    /// <summary>Builds a local return URL without stale localization query values.</summary>
    string BuildCultureReturnUrl(string absoluteUri);

    /// <summary>Adds an explicit request culture to one validated local return URL.</summary>
    string BuildCultureRedirectUrl(string? returnUrl, string culture);

    /// <summary>Builds the endpoint URL used to select and persist one application culture.</summary>
    string BuildCultureSelectionUrl(string absoluteUri, string culture);

    /// <summary>Saves persistent user overrides for one flat JSON localization catalog.</summary>
    Task SaveOverridesAsync(string culture, IReadOnlyDictionary<string, string> strings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the render export catalog service contract.
/// </summary>
public interface IRenderExportCatalogService
{
    /// <summary>
    /// Gets capabilities.
    /// </summary>
    IReadOnlyList<RenderExportCapability> GetCapabilities();
    /// <summary>
    /// Runs the find operation.
    /// </summary>
    RenderExportCapability? Find(string format);
}
