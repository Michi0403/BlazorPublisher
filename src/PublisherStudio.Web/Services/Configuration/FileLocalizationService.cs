using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates file localization behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="environment">Web host environment dependency used by the file localization workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class FileLocalizationService(IWebHostEnvironment environment, ILogger<FileLocalizationService> logger) : IFileLocalizationService
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="FileLocalizationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    /// <summary>
    /// Stores the in-memory cache collection maintained internally by <see cref="FileLocalizationService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Stores the canonical English text-to-key index used by media studios and other source-owned UI surfaces.</summary>
    private IReadOnlyDictionary<string, string>? _englishKeysByText;
    /// <summary>
    /// Gets the localization path used by this file localization instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The localization path value exposed by <see cref="FileLocalizationService"/>.</value>
    private string LocalizationPath => Path.Combine(environment.ContentRootPath, "Localization");
    /// <summary>
    /// Gets the override path used by this file localization instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The override path value exposed by <see cref="FileLocalizationService"/>.</value>
    private string OverridePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "LocalizationOverrides");

    /// <summary>
    /// Retrieves available cultures as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<string> GetAvailableCultures()
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.GetAvailableCultures.");
                    var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "en-US" };
                    AddCultures(LocalizationPath, candidates);
                    AddCultures(OverridePath, candidates);
                    var requiredKeys = LoadFile(Path.Combine(LocalizationPath, "en-US.json")).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var available = candidates.Where(culture =>
                    {
                        if (string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase)) return true;
                        var translated = LoadFile(Path.Combine(LocalizationPath, culture + ".json"));
                        foreach (var pair in LoadFile(Path.Combine(OverridePath, culture + ".json"))) translated[pair.Key] = pair.Value;
                        return requiredKeys.All(translated.ContainsKey);
                    });
                    return available.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.GetAvailableCultures failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves strings as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The i read only dictionary string string produced by the operation.</returns>
    public IReadOnlyDictionary<string, string> GetStrings(string? culture = null)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.GetStrings.");
                    var requested = NormalizeCulture(culture);
                    return _cache.GetOrAdd(requested, Load);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.GetStrings failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs get as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Get(string key, string? culture = null, string? fallback = null)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.Get.");
                    if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;
                    var requested = GetStrings(culture);
                    if (requested.TryGetValue(key, out var translated)) return translated;
                    var neutral = NormalizeCulture(culture).Split('-', 2)[0];
                    var neutralFile = GetAvailableCultures().FirstOrDefault(item => item.StartsWith(neutral + "-", StringComparison.OrdinalIgnoreCase));
                    if (neutralFile is not null && GetStrings(neutralFile).TryGetValue(key, out translated)) return translated;
                    if (GetStrings("en-US").TryGetValue(key, out translated)) return translated;
                    return fallback ?? key;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.Get failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Resolves one canonical English UI literal through the localization catalogs.
    /// </summary>
    /// <param name="englishText">Canonical English UI text stored in the source catalog.</param>
    /// <param name="culture">Optional culture to resolve; the current UI culture is used when omitted.</param>
    /// <returns>The translated catalog value, or the input text when it is not catalogued.</returns>
    public string GetText(string englishText, string? culture = null)
    {
        try
        {
            logger.LogTrace("Resolving localized UI text from its canonical English value.");
            if (string.IsNullOrEmpty(englishText)) return englishText ?? string.Empty;
            var index = _englishKeysByText;
            if (index is null)
            {
                var built = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var pair in LoadFile(Path.Combine(LocalizationPath, "en-US.json")).OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(pair.Value) && !built.ContainsKey(pair.Value)) built[pair.Value] = pair.Key;
                }
                _englishKeysByText = built;
                index = built;
            }
            return index.TryGetValue(englishText, out var key) ? Get(key, culture, englishText) : englishText;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "FileLocalizationService.GetText failed while resolving {EnglishText}.", englishText);
            throw;
        }
    }


    /// <summary>
    /// Resolves available culture as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ResolveAvailableCulture(string? culture)
    {
        try
        {
            logger.LogTrace("Resolving the requested PublisherStudio culture.");
            var normalized = NormalizeCulture(culture);
            return GetAvailableCultures().FirstOrDefault(item =>
                string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)) ?? "en-US";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio culture resolution failed.");
            throw;
        }
    }

    /// <summary>
    /// Gets the native display name and normalized culture name for one available catalog.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetCultureDisplayName(string culture)
    {
        try
        {
            logger.LogTrace("Creating the display label for PublisherStudio culture {Culture}.", culture);
            var selected = ResolveAvailableCulture(culture);
            var info = CultureInfo.GetCultureInfo(selected);
            return $"{info.NativeName} ({info.Name})";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio culture display-name creation failed for {Culture}.", culture);
            throw;
        }
    }

    /// <summary>
    /// Builds a local return URL while removing stale culture query values.
    /// </summary>
    /// <param name="absoluteUri">Absolute uri value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string BuildCultureReturnUrl(string absoluteUri)
    {
        try
        {
            logger.LogTrace("Building the PublisherStudio culture return URL.");
            if (!Uri.TryCreate(absoluteUri, UriKind.Absolute, out var current)) return "/";
            return BuildCultureUrl(current.AbsolutePath, current.Query, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio culture return URL creation failed.");
            throw;
        }
    }

    /// <summary>
    /// Adds an explicit request culture to one validated local return URL.
    /// </summary>
    /// <param name="returnUrl">Return url value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string BuildCultureRedirectUrl(string? returnUrl, string culture)
    {
        try
        {
            logger.LogTrace("Building the PublisherStudio culture redirect URL.");
            var selected = ResolveAvailableCulture(culture);
            var local = string.IsNullOrWhiteSpace(returnUrl)
                || !returnUrl.StartsWith("/", StringComparison.Ordinal)
                || returnUrl.StartsWith("//", StringComparison.Ordinal)
                    ? "/"
                    : returnUrl;
            if (!Uri.TryCreate("http://publisherstudio.invalid" + local, UriKind.Absolute, out var parsed))
                return "/?culture=" + Uri.EscapeDataString(selected) + "&ui-culture=" + Uri.EscapeDataString(selected);
            return BuildCultureUrl(parsed.AbsolutePath, parsed.Query, selected);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio culture redirect URL creation failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds the application endpoint used to persist and select one culture.
    /// </summary>
    /// <param name="absoluteUri">Absolute uri value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string BuildCultureSelectionUrl(string absoluteUri, string culture)
    {
        try
        {
            logger.LogTrace("Building the PublisherStudio culture-selection endpoint.");
            var selected = ResolveAvailableCulture(culture);
            var returnUrl = BuildCultureReturnUrl(absoluteUri);
            var endpoint = QueryHelpers.AddQueryString("/api/configuration/localization/select", "culture", selected);
            return QueryHelpers.AddQueryString(endpoint, "returnUrl", returnUrl);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio culture-selection endpoint creation failed.");
            throw;
        }
    }

    /// <summary>
    /// Persists overrides as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="strings">String dependency used by the file localization workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task SaveOverridesAsync(string culture, IReadOnlyDictionary<string, string> strings, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.SaveOverridesAsync.");
                    var normalized = NormalizeCulture(culture);
                    Directory.CreateDirectory(OverridePath);
                    var clean = strings
                        .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                        .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                    var path = Path.Combine(OverridePath, normalized + ".json");
                    var stream = File.Create(path);
                    await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
                    await JsonSerializer.SerializeAsync(stream, clean, JsonOptions, cancellationToken).ConfigureAwait(false);
                    _cache.TryRemove(normalized, out _);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.SaveOverridesAsync failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs load as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The i read only dictionary string string produced by the operation.</returns>
    private IReadOnlyDictionary<string, string> Load(string culture)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.Load.");
                    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    MergeFile(Path.Combine(LocalizationPath, "en-US.json"), result);
                    if (!string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase))
                        MergeFile(Path.Combine(LocalizationPath, culture + ".json"), result);
                    MergeFile(Path.Combine(OverridePath, culture + ".json"), result);
                    return result;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.Load failed: {exception.Message}");
            throw;
        }
    }


    /// <summary>
    /// Loads file as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The dictionary string string produced by the operation.</returns>
    private Dictionary<string, string> LoadFile(string path)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.LoadFile.");
                    if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    using var stream = File.OpenRead(path);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                        ?? new Dictionary<string, string>();
                    var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in data.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)))
                    {
                        if (normalized.ContainsKey(pair.Key))
                            logger.LogWarning(
                                "Localization catalog {CatalogPath} contains a case-insensitive duplicate key {LocalizationKey}; the later value is used defensively. Source-controlled catalogs must still pass the localization integrity guard.",
                                path,
                                pair.Key);
                        normalized[pair.Key] = pair.Value ?? string.Empty;
                    }
                    return normalized;

        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.LoadFile failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs merge file as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="result">String dependency used by the file localization workflow to provide the corresponding application capability.</param>
    private void MergeFile(string path, IDictionary<string, string> result)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.MergeFile.");
                    if (!File.Exists(path)) return;
                    using var stream = File.OpenRead(path);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                    if (data is null) return;
                    foreach (var pair in data.Where(pair => !string.IsNullOrWhiteSpace(pair.Key))) result[pair.Key] = pair.Value ?? string.Empty;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.MergeFile failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds cultures as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="values">String dependency used by the file localization workflow to provide the corresponding application capability.</param>
    private void AddCultures(string path, ISet<string> values)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.AddCultures.");
                    if (!Directory.Exists(path)) return;
                    foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        if (!string.IsNullOrWhiteSpace(name)) values.Add(name);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.AddCultures failed: {exception.Message}");
            throw;
        }
    }


    /// <summary>
    /// Builds one local route while preserving query values unrelated to localization.
    /// </summary>
    /// <param name="absolutePath">Absolute path value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="query">Query value supplied to the file localization operation and used when producing its result.</param>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildCultureUrl(string absolutePath, string query, string? culture)
    {
        try
        {
            logger.LogTrace("Building a local PublisherStudio culture route.");
            var result = string.IsNullOrWhiteSpace(absolutePath) ? "/" : absolutePath;
            foreach (var pair in QueryHelpers.ParseQuery(query))
            {
                if (string.Equals(pair.Key, "culture", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(pair.Key, "ui-culture", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var value in pair.Value)
                {
                    if (value is null) continue;
                    result = QueryHelpers.AddQueryString(result, pair.Key, value);
                }
            }

            if (!string.IsNullOrWhiteSpace(culture))
            {
                result = QueryHelpers.AddQueryString(result, "culture", culture);
                result = QueryHelpers.AddQueryString(result, "ui-culture", culture);
            }

            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Local PublisherStudio culture route creation failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes culture as part of the file localization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="culture">Culture value supplied to the file localization operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeCulture(string? culture)
    {
        try
        {
            logger.LogTrace($"Entering FileLocalizationService.NormalizeCulture.");
                    var requested = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentUICulture.Name : culture.Trim();
                    try { return CultureInfo.GetCultureInfo(requested).Name; }
                    catch (CultureNotFoundException) { return "en-US"; }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"FileLocalizationService.NormalizeCulture failed: {exception.Message}");
            throw;
        }
    }
}
