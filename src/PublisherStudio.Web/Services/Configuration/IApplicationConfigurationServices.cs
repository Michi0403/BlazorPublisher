using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the contract for application path behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IApplicationPathService
{
    /// <summary>
    /// Retrieves defaults as part of the application path service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The PublisherStudio path options produced by the operation.</returns>
    PublisherStudioPathOptions GetDefaults();
    /// <summary>
    /// Performs resolve as part of the application path service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectOverrides">Project overrides value supplied to the application path operation and used when producing its result.</param>
    /// <returns>The PublisherStudio path options produced by the operation.</returns>
    PublisherStudioPathOptions Resolve(PublisherStudioPathOptions? projectOverrides = null);
    /// <summary>
    /// Resolves media path as part of the application path service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="mediaKind">Media kind value supplied to the application path operation and used when producing its result.</param>
    /// <param name="projectOverrides">Project overrides value supplied to the application path operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string ResolveMediaPath(string mediaKind, PublisherStudioPathOptions? projectOverrides = null);
    /// <summary>
    /// Ensures directories as part of the application path service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectOverrides">Project overrides value supplied to the application path operation and used when producing its result.</param>
    void EnsureDirectories(PublisherStudioPathOptions? projectOverrides = null);
}

/// <summary>
/// Defines the contract for file localization behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IFileLocalizationService
{
    /// <summary>
    /// Retrieves available cultures as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<string> GetAvailableCultures();

    /// <summary>
    /// Retrieves strings as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The i read only dictionary string string produced by the operation.</returns>
    IReadOnlyDictionary<string, string> GetStrings(string? culture = null);

    /// <summary>Gets one localized string with English and caller-provided fallback behavior.</summary>
    /// <param name="key">Key value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string Get(string key, string? culture = null, string? fallback = null);

    /// <summary>Resolves a source-owned English UI literal through the localization catalogs without duplicating localization keys in components.</summary>
    /// <param name="englishText">Canonical English UI text stored in the source localization catalog.</param>
    /// <param name="culture">Optional culture to resolve; the current UI culture is used when omitted.</param>
    /// <returns>The localized text when the English source value is catalogued; otherwise the original text.</returns>
    string GetText(string englishText, string? culture = null);

    /// <summary>
    /// Resolves available culture as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string ResolveAvailableCulture(string? culture);

    /// <summary>
    /// Retrieves culture display name as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetCultureDisplayName(string culture);

    /// <summary>
    /// Builds culture return URL as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="absoluteUri">Absolute uri value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildCultureReturnUrl(string absoluteUri);

    /// <summary>Adds an explicit request culture to one validated local return URL.</summary>
    /// <param name="returnUrl">Return url value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildCultureRedirectUrl(string? returnUrl, string culture);

    /// <summary>Builds the endpoint URL used to select and persist one application culture.</summary>
    /// <param name="absoluteUri">Absolute uri value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildCultureSelectionUrl(string absoluteUri, string culture);

    /// <summary>Saves persistent user overrides for one flat JSON localization catalog.</summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="strings">String dependency used by the file localization workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task SaveOverridesAsync(string culture, IReadOnlyDictionary<string, string> strings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for render export catalog behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRenderExportCatalogService
{
    /// <summary>
    /// Retrieves capabilities as part of the render export catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<RenderExportCapability> GetCapabilities();
    /// <summary>
    /// Performs find as part of the render export catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="format">Format value supplied to the render export catalog operation and used when producing its result.</param>
    /// <returns>The render export capability produced by the operation.</returns>
    RenderExportCapability? Find(string format);
}
