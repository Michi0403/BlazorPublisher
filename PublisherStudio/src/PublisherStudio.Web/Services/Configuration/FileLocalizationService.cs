using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

public sealed class FileLocalizationService(IWebHostEnvironment environment) : IFileLocalizationService
{
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private string LocalizationPath => Path.Combine(environment.ContentRootPath, "Localization");

    public IReadOnlyList<string> GetAvailableCultures()
    {
        if (!Directory.Exists(LocalizationPath)) return ["en-US"];
        return Directory.EnumerateFiles(LocalizationPath, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList().AsReadOnly();
    }

    public IReadOnlyDictionary<string, string> GetStrings(string? culture = null)
    {
        var requested = NormalizeCulture(culture);
        return _cache.GetOrAdd(requested, Load);
    }

    public string Get(string key, string? culture = null, string? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;
        var requested = GetStrings(culture);
        if (requested.TryGetValue(key, out var translated)) return translated;
        var neutral = NormalizeCulture(culture).Split('-', 2)[0];
        var neutralFile = GetAvailableCultures().FirstOrDefault(item => item.StartsWith(neutral + "-", StringComparison.OrdinalIgnoreCase));
        if (neutralFile is not null && GetStrings(neutralFile).TryGetValue(key, out translated)) return translated;
        if (GetStrings("en-US").TryGetValue(key, out translated)) return translated;
        return fallback ?? key;
    }

    private IReadOnlyDictionary<string, string> Load(string culture)
    {
        var path = Path.Combine(LocalizationPath, culture + ".json");
        if (!System.IO.File.Exists(path) && !string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase))
            path = Path.Combine(LocalizationPath, "en-US.json");
        if (!System.IO.File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var stream = System.IO.File.OpenRead(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
    }

    private string NormalizeCulture(string? culture)
    {
        var requested = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentUICulture.Name : culture.Trim();
        try { return CultureInfo.GetCultureInfo(requested).Name; }
        catch (CultureNotFoundException) { return "en-US"; }
    }
}
