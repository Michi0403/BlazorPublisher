using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

public sealed class FileLocalizationService(IWebHostEnvironment environment) : IFileLocalizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private string LocalizationPath => Path.Combine(environment.ContentRootPath, "Localization");
    private string OverridePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "LocalizationOverrides");

    public IReadOnlyList<string> GetAvailableCultures()
    {
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

    public async Task SaveOverridesAsync(string culture, IReadOnlyDictionary<string, string> strings, CancellationToken cancellationToken = default)
    {
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

    private IReadOnlyDictionary<string, string> Load(string culture)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        MergeFile(Path.Combine(LocalizationPath, "en-US.json"), result);
        if (!string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase))
            MergeFile(Path.Combine(LocalizationPath, culture + ".json"), result);
        MergeFile(Path.Combine(OverridePath, culture + ".json"), result);
        return result;
    }


    private static Dictionary<string, string> LoadFile(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var stream = File.OpenRead(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();
        return data.Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private static void MergeFile(string path, IDictionary<string, string> result)
    {
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        if (data is null) return;
        foreach (var pair in data.Where(pair => !string.IsNullOrWhiteSpace(pair.Key))) result[pair.Key] = pair.Value ?? string.Empty;
    }

    private static void AddCultures(string path, ISet<string> values)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!string.IsNullOrWhiteSpace(name)) values.Add(name);
        }
    }

    private static string NormalizeCulture(string? culture)
    {
        var requested = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentUICulture.Name : culture.Trim();
        try { return CultureInfo.GetCultureInfo(requested).Name; }
        catch (CultureNotFoundException) { return "en-US"; }
    }
}
