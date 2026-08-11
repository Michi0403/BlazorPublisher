using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Provides file localization service operations.
/// </summary>
public sealed class FileLocalizationService(IWebHostEnvironment environment, ILogger<FileLocalizationService> logger) : IFileLocalizationService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private string LocalizationPath => Path.Combine(environment.ContentRootPath, "Localization");
    private string OverridePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "LocalizationOverrides");

    /// <summary>
    /// Gets available cultures.
    /// </summary>
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
    /// Gets strings.
    /// </summary>
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
    /// Runs the get operation.
    /// </summary>
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
    /// Resolves a requested culture to one complete PublisherStudio localization catalog.
    /// </summary>
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
    /// Saves overrides async.
    /// </summary>
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
                    await using var stream = File.Create(path);
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
    /// Runs the load operation.
    /// </summary>
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
    /// Loads file.
    /// </summary>
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
    /// Runs the merge file operation.
    /// </summary>
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
    /// Adds cultures.
    /// </summary>
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
    /// Normalizes culture.
    /// </summary>
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
